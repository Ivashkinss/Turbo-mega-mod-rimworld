$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$build=Join-Path $PSScriptRoot 'Build/RegionAllocationChecks'
New-Item -ItemType Directory -Path $build -Force | Out-Null
$source=Join-Path $build 'Check.cs'
@"
using System;
using System.Linq;
using TurboMegaMod;
public static class CheckRegions {
 static int checks;
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;}
 static double Cost(double[,] c,int[] g,int[] a){return Enumerable.Range(0,g.Length).Sum(i=>c[g[i],a[i]]);}
 static double Best(double[,] c,int[] g,int row,bool[] used){if(row==g.Length)return 0;double best=double.PositiveInfinity;for(int j=0;j<g.Length;j++){if(used[j]||c[g[row],j]>=WorldRegionAllocation.Forbidden)continue;used[j]=true;best=Math.Min(best,c[g[row],j]+Best(c,g,row+1,used));used[j]=false;}return best;}
 static void Valid(double[,] c,int[] g,int[] a){Check(a.Length==g.Length&&a.Distinct().Count()==a.Length,"Not a permutation");Check(Enumerable.Range(0,g.Length).All(i=>c[g[i],a[i]]<WorldRegionAllocation.Forbidden),"Forbidden climate/site assignment");}
 public static void Main(){
  Check(WorldRegionAllocation.Assign(new double[0,0],new int[0]).Length==0,"Empty case");
  for(int seed=0;seed<6;seed++){
   int n=7;var rng=new Random(seed+391);var g=Enumerable.Range(0,n).Select(i=>i%3).ToArray();var c=new double[3,n];
   for(int k=0;k<3;k++)for(int j=0;j<n;j++)c[k,j]=k!=g[j]&&rng.Next(4)==0?WorldRegionAllocation.Forbidden:rng.Next(1,300);
   var a=WorldRegionAllocation.Assign(c,g);Valid(c,g,a);Check(Math.Abs(Cost(c,g,a)-Best(c,g,0,new bool[n]))<0.00001,"Not globally optimal against independent exhaustive oracle");Check(a.SequenceEqual(WorldRegionAllocation.Assign(c,g)),"Assignment not deterministic");
  }
  {int n=400;var g=Enumerable.Range(0,n).Select(i=>i%4).ToArray();var c=new double[4,n];for(int k=0;k<4;k++)for(int j=0;j<n;j++)c[k,j]=Math.Abs(j-(k*100+50));var a=WorldRegionAllocation.Assign(c,g);Valid(c,g,a);Check(Cost(c,g,a)<Cost(c,g,Enumerable.Range(0,n).ToArray()),"Bounded large-world optimization did not improve");}
  Console.WriteLine("PASS: "+checks+" checks; exact production code vs exhaustive small-world oracle and bounded400-site allocation.");
 }
}
"@ | Set-Content -LiteralPath $source -Encoding UTF8
$exe=Join-Path $build 'Check.exe'
& (Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe') /nologo /target:exe ('/out:'+$exe) (Join-Path $root 'Source/WorldRegionAllocation.cs') $source
if($LASTEXITCODE -ne 0){throw 'Region solver check compile failed.'}
& $exe
if($LASTEXITCODE -ne 0){throw 'Region solver verification failed.'}
