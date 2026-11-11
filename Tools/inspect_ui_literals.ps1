param([Parameter(Mandatory=$true)][string]$AssemblyPath, [switch]$AllMethods,
      [string]$TypePattern = '.*', [string]$MethodPattern = '.*', [switch]$IncludeInstructions)
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
function Get-TypeName($handle) {
    $definition = $reader.GetTypeDefinition($handle)
    $parent = $definition.GetDeclaringType()
    if (!$parent.IsNil) { return (Get-TypeName $parent) + '+' + $reader.GetString($definition.Name) }
    return ($reader.GetString($definition.Namespace) + '.' + $reader.GetString($definition.Name)).TrimStart('.')
}
function Get-CallName([int]$token) {
    $row = $token -band 16777215
    switch ($token -shr 24) {
        4 {
            $definition = $reader.GetFieldDefinition([Reflection.Metadata.Ecma335.MetadataTokens]::FieldDefinitionHandle($row))
            return (Get-TypeName $definition.GetDeclaringType()) + '::' + $reader.GetString($definition.Name)
        }
        43 {
            $spec = $reader.GetMethodSpecification([Reflection.Metadata.Ecma335.MetadataTokens]::MethodSpecificationHandle($row))
            return Get-CallName ([Reflection.Metadata.Ecma335.MetadataTokens]::GetToken($spec.Method))
        }
        6 {
            $definition = $reader.GetMethodDefinition([Reflection.Metadata.Ecma335.MetadataTokens]::MethodDefinitionHandle($row))
            return (Get-TypeName $definition.GetDeclaringType()) + '::' + $reader.GetString($definition.Name)
        }
        10 {
            $member = $reader.GetMemberReference([Reflection.Metadata.Ecma335.MetadataTokens]::MemberReferenceHandle($row))
            $parentToken = [Reflection.Metadata.Ecma335.MetadataTokens]::GetToken($member.Parent)
            $parentName = ''
            if (($parentToken -shr 24) -eq 1) {
                $parent = $reader.GetTypeReference([Reflection.Metadata.Ecma335.MetadataTokens]::TypeReferenceHandle($parentToken -band 16777215))
                $parentName = $reader.GetString($parent.Namespace) + '.' + $reader.GetString($parent.Name)
            }
            return $parentName + '::' + $reader.GetString($member.Name)
        }
    }
    return $token.ToString('X8')
}
try {
    foreach ($typeHandle in $reader.TypeDefinitions) {
        $type = $reader.GetTypeDefinition($typeHandle)
        $typeName = Get-TypeName $typeHandle
        if ($typeName -notmatch $TypePattern) { continue }
        foreach ($methodHandle in $type.GetMethods()) {
            $method = $reader.GetMethodDefinition($methodHandle)
            if ($reader.GetString($method.Name) -notmatch $MethodPattern) { continue }
            if (!$method.RelativeVirtualAddress) { continue }
            $bytes = [Reflection.Metadata.PEReaderExtensions]::GetMethodBody($pe,$method.RelativeVirtualAddress).GetILBytes()
            $literals = @()
            $calls = @()
            $instructions = @()
            $cursor = 0
            while ($cursor -lt $bytes.Length) {
                $number = [int]$bytes[$cursor++]
                if ($number -eq 254) { $number = 65024 + [int]$bytes[$cursor++] }
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
                    $calls += Get-CallName $token
                }
                if ($IncludeInstructions) {
                    $operand = if ($length -eq 4) { [BitConverter]::ToInt32($bytes,$cursor).ToString('X8') } else { '' }
                    if ($opcode.OperandType -in @('InlineMethod','InlineField')) { $operand = Get-CallName ([BitConverter]::ToInt32($bytes,$cursor)) }
                    elseif ($opcode.OperandType -eq 'ShortInlineI') { $operand = [string]([int]$bytes[$cursor] - $(if ($bytes[$cursor] -gt 127) {256} else {0})) }
                    $instructions += ($opcode.Name + ' ' + $operand).Trim()
                }
                $cursor += $length
            }
            if ($AllMethods -or ($literals.Count -and ($calls -match 'Label|Button|Tooltip|Checkbox|TextField|Translate|Radio|FloatMenu|Dialog|Header|Slider'))) {
                $row = [ordered]@{Type=$typeName; Method=$reader.GetString($method.Name); Strings=$literals; Calls=@($calls | Select-Object -Unique)}
                if ($IncludeInstructions) { $row.Instructions = $instructions }
                $results += [pscustomobject]$row
            }
        }
    }
} finally { $pe.Dispose(); $stream.Dispose() }
$results | ConvertTo-Json -Depth 5
