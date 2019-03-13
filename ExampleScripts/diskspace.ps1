$drives=Get-WmiObject Win32_LogicalDisk

foreach ($drive in $drives){         
    $drivename = $drive.DeviceID
    $freespace = [int]($drive.FreeSpace/1GB)
    $totalspace = [int]($drive.Size/1GB)
    $usedspace = $totalspace - $freespace
    Write-Output "$drivename drive - $($usedspace)GB used, $($freespace)GB free, $($totalspace)GB total"
}