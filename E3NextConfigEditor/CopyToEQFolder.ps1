
if($Env:E3BuildDest)
{
    #Set-ExecutionPolicy -Scope CurrentUser Bypass
    $ValidPath = Test-Path -Path "$Env:E3BuildDest"
    if($ValidPath)
    {
        Copy-Item -Path "*.dll" -Destination "$Env:E3BuildDest"
        Copy-Item -Path "*.exe" -Destination "$Env:E3BuildDest"
        Copy-Item -Path "*.exe.config" -Destination "$Env:E3BuildDest"
    }

    
}
