
if($Env:E3BuildDest)
{
    $ValidPath = Test-Path -Path "$Env:E3BuildDest"
    if($ValidPath)
    {
        Copy-Item -Path "*.*" -Destination "$Env:E3BuildDest"
    }

    
}
