# Internet of Powershell (IoPS)
[![Build Status](https://scholden.visualstudio.com/IoPS/_apis/build/status/ScottHolden.IoPS?branchName=master)](https://scholden.visualstudio.com/IoPS/_build/latest?definitionId=26&branchName=master)  
This is a quick proof a concept adapter for allowing IoT Hub to execute curated Powershell scripts on devices via direct method.

## To Run In Interactive Mode
- Build the project (Clone, Build as Release) or grab a copy via the 'Releases' link above
- Configure the `IoPS.exe.config` file with the target script path (`scriptpath`), and IoT Hub Device connection string (`iothub`)
- Run `IoPS.exe`, this will open up an interactive copy with logging

## To Install As A Service
- Follow the steps above to make sure it is working as expected
- As an administrator, run `IoPS.exe --install` to install as a service
- To uninstall, As an administrator, run `IoPS.exe --uninstall`

## Calling Scripts From IoT Hub
- There are 2 direct methods avalible to call:
  - `ListScripts` - This will list any ps1 files with the correct naming format in the script path. This direct method does not need a body.
  - `ExecuteScript` - This will execute a target script. The payload body is made up of:
    - `ScriptName` the name of the script to run
    - *Optional* `ExecutionPolicy` string defining what execution policy mode to set for the process. By **default** this is set to `RemoteSigned`. Options are `Unrestricted`, `RemoteSigned`, `AllSigned`, `Restricted`, `Default`, `Bypass`, or `Undefined`
    - *Optional* `Parameters` object containing paramaters that should be passed into the script
    ```
    {
      "ScriptName":"echo.ps1"
    }
    ```
    - Just passing in the script name is the equivilant of executing `./echo.ps1` with execution policy mode `RemoteSigned`
    ```
    {
      "ScriptName":"calc.ps1",
      "ExecutionPolicy":"Bypass",
      "Parameters":
      {
        "a":2,
        "b":3
      }
    }
    ```
    - The example above is the equivilant of executing `./calc.ps1 -a 2 -b 3` with execution policy mode `Bypass`
- *Note: Write-Host won't work, you can use Write-Output or return a value to pass information back*