# Internet of Powershell (IoPS)
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
  - `ExecuteScript` - This will execute a target script. The payload body is made up of the `ScriptName` and an optional `Parameters` object:
  ```
  {
    "ScriptName":"calc.ps1",
    "Parameters":
    {
      "a":2,
      "b":3
    }
  }
  ```
- The example above is the equivilant of executing `./calc.ps1 -a 2 -b 3`
- *Note: Write-Host won't work, you can use Write-Output or return a value to pass information back*