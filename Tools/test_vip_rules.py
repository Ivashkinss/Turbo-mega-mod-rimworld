"""Run independently reviewed vectors against the actual C# rule source."""
import hashlib
import json
import math
import os
from pathlib import Path
import subprocess

root = Path(__file__).resolve().parents[1]
cases = json.loads((root / 'Tests/VipLoyaltyCases.json').read_text(encoding='utf-8'))
build = root / 'Tools/Build'
build.mkdir(exist_ok=True)

def literal(value, single=False):
    if value is None:
        return 'float.NaN' if single else 'double.NaN'
    if isinstance(value, bool):
        return str(value).lower()
    if isinstance(value, str):
        return json.dumps(value)
    return repr(value) + ('f' if single else '')

lines = ['using System; using TurboMegaMod; class VipRuleTest { static int Main() { int n=0;']
for c in cases:
    inputs=c['inputs']; name=c['function']
    args=', '.join(literal(v, name=='Good') for v in inputs.values())
    lines.append(f'if(GuestProfileRules.{name}({args}) != {literal(c["expected"])}) throw new Exception("case {c["id"]}"); n++;')
lines.append('Console.WriteLine(n+" actual C# vectors passed"); return 0; } }')
source=build/'VipRuleTest.cs'; source.write_text('\n'.join(lines),encoding='utf-8')
exe=build/'VipRuleTest.exe'
compiler=Path(os.environ['WINDIR'])/'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
subprocess.run([str(compiler),'/nologo','/target:exe','/out:'+str(exe),str(root/'Source/GuestProfileRules.cs'),str(source)],check=True)
subprocess.run([str(exe)],check=True)
result={'passed':True,'cases':len(cases),'sourceSha256':hashlib.sha256((root/'Source/GuestProfileRules.cs').read_bytes()).hexdigest(),'vectorsSha256':hashlib.sha256((root/'Tests/VipLoyaltyCases.json').read_bytes()).hexdigest(),'gameTest':False}
(root/'Documentation/VipRuleVerification.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
