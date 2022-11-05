
if($Env:E3BuildDest)
{
    $ValidPath = Test-Path -Path "$Env:E3BuildDest"
    if($ValidPath)
    {
        Copy-Item -Path "*.dll" -Destination "$Env:E3BuildDest"
        Copy-Item -Path "*.exe" -Destination "$Env:E3BuildDest"
    }

    
}
