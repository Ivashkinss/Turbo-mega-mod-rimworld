param([Parameter(Mandatory=$true)][string]$AssemblyPath)
$ErrorActionPreference = 'Stop'
$opcodeMap = @{}
foreach ($field in [Reflection.Emit.OpCodes].GetFields([Reflection.BindingFlags]'Static,Public')) {
    $opcode = $field.GetValue($null)
    $opcodeMap[[int]$opcode.Value -band 65535] = $opcode
}
$stream = [IO.File]::OpenRead((Resolve-Path -LiteralPath $AssemblyPath))
$pe = [Reflection.PortableExecutable.PEReader]::new($stream)
$reader = [Reflection.Metadata.PEReaderExtensions]::GetMetadataReader($pe)
$results = @()
try {
    foreach ($typeHandle in $reader.TypeDefinitions) {
        $type = $reader.GetTypeDefinition($typeHandle)
        $typeName = $reader.GetString($type.Namespace) + '.' + $reader.GetString($type.Name)
        foreach ($methodHandle in $type.GetMethods()) {
            $method = $reader.GetMethodDefinition($methodHandle)
            if (!$method.RelativeVirtualAddress) { continue }
            $bytes = [Reflection.Metadata.PEReaderExtensions]::GetMethodBody($pe,$method.RelativeVirtualAddress).GetILBytes()
            $literals = @()
            $calls = @()
            $cursor = 0
            while ($cursor -lt $bytes.Length) {
                $number = [int]$bytes[$cursor++]
                if ($number -eq 254) { $number = 65280 + [int]$bytes[$cursor++] }
                $opcode = $opcodeMap[$number]
                $length = switch ([string]$opcode.OperandType) {
                    'InlineNone' {0}
                    'ShortInlineI' {1}
                    'ShortInlineVar' {1}
                    'ShortInlineBrTarget' {1}
                    'InlineVar' {2}
                    'InlineI8' {8}
                    'InlineR' {8}
                    'InlineSwitch' {4 + 4 * [BitConverter]::ToInt32($bytes,$cursor)}
                    default {4}
                }
                if ($opcode.Name -eq 'ldstr') {
                    $token = [BitConverter]::ToInt32($bytes,$cursor)
                    $literal = $reader.GetUserString([Reflection.Metadata.Ecma335.MetadataTokens]::UserStringHandle($token -band 16777215))
                    if ($literal -match '[A-Za-z]' -and $literal -notmatch '^(AM\.|SimpleSettings\.)') { $literals += $literal }
                }
                if ($opcode.Name -in @('call','callvirt','newobj')) {
                    $token = [BitConverter]::ToInt32($bytes,$cursor)
                    if (($token -shr 24) -eq 10) {
                        $member = $reader.GetMemberReference([Reflection.Metadata.Ecma335.MetadataTokens]::MemberReferenceHandle($token -band 16777215))
                        $memberName = $reader.GetString($member.Name)
                        $calls += $memberName
                    }
                }
                $cursor += $length
            }
            if ($literals.Count -and ($calls -match 'Label|Button|Tooltip|Checkbox|TextField|Translate|Radio|FloatMenu|Dialog|Header|Slider')) {
                $results += [pscustomobject]@{Type=$typeName; Method=$reader.GetString($method.Name); Strings=$literals; Calls=@($calls | Select-Object -Unique)}
            }
        }
    }
} finally { $pe.Dispose(); $stream.Dispose() }
$results | ConvertTo-Json -Depth 5
