
##
## This is set of convenience scripts to help populate
## the email and/or slack tags for IAM user accounts.
##
## The first script can be used to export all the existing
## users and their current email/slack tags, optionally
## adding a blank tag if an existing value doesn't exist.
## This list will be exported to a JSON file.
##
## You can then edit this JSON file and populate or update
## the values for any users you want to update, and remove
## all the users you don't need to touch.
##
## Finally, then you can merge in the updates into your
## existing IAM user set, by reading the updates from the
## updated JSON file.  You can also run the tool in a WHATIF
## scenario (-WhatIf) to see what changes would be made.
##

function Export-UserTags {
    param(
        [Parameter(Mandatory)]
        [string]$ProfileName,
        [string]$Region="us-east-1",
        [string]$Path="./UserAndTags.json",
        [switch]$AddMissingEmail,
        [switch]$AddMissingSlack,
        [switch]$Overwrite
    )

    $fullPath = [System.IO.Path]::GetFullPath(
        [System.IO.Path]::Combine($PWD, $Path))
    Write-Host "Writing to [$fullPath]"
    if ([System.IO.File]::Exists($fullPath) -and -not $Overwrite) {
        Write-Error "Found existing file at: $fullPath; specify -OVerwrite to overwrite"
        return
    }

    $users =  Get-IAMUserList -ProfileName $ProfileName -Region $Region

    if ($AddMissingEmail -or $AddMissingSlack) {
        foreach ($u in $users) {
            $t = $u.Tags
            $e = $t | Where-Object { $_.Key -eq 'email' }
            $s = $t | Where-Object { $_.Key -eq 'slack' }
            if ($AddMissingEmail -and -not $e) {
                $t.Add(@{ Key = "email"; Value = ""})
            }
            if ($AddMissingSlack -and -not $s) {
                $t.Add(@{ Key = "slack"; Value = ""})
            }
        }
    }

    $users | Select-Object username,tags | ConvertTo-Json -Depth 5 > $Path
}

function Merge-UserTags {
    param(
        [Parameter(Mandatory)]
        [string]$ProfileName,
        [string]$Region="us-east-1",
        [string]$Path="./UserAndTags.json",
        [switch]$OverwriteTags,
        [switch]$WhatIf
    )

    $awsCreds = @{
        ProfileName = $ProfileName
        Region      = $Region
    }

    $fullPath = [System.IO.Path]::GetFullPath(
        [System.IO.Path]::Combine($PWD, $Path))
    Write-Host "Reading from [$fullPath]"
    if (-not [System.IO.File]::Exists($fullPath)) {
        Write-Error "Missing file at: $fullPath"
        return
    }

    $usersJson = [System.IO.File]::ReadAllText($fullPath)
    $users = ConvertFrom-Json $usersJson
    Write-Host "Read in [$($users.Count)] user(s)."

    foreach ($u in $users) {
        $username = $u.UserName
        $allTags = $u.Tags
        $newEmail = $u.Tags | Where-Object { $_.Key -eq 'email' } | Select-Object -ExpandProperty Value
        $newSlack = $u.Tags | Where-Object { $_.Key -eq 'slack' } | Select-Object -ExpandProperty Value

        #Write-Host "[$username]:  $newEmail, $newSlack"

        $iamUser = Get-IAMUser -UserName $username @awsCreds
        $oldEmail = $iamUser.Tags | Where-Object { $_.Key -eq 'email' } | Select-Object -ExpandProperty Value
        $oldSlack = $iamUser.Tags | Where-Object { $_.Key -eq 'slack' } | Select-Object -ExpandProperty Value

        $changes = @(
            @{ name = "Email"; tag = "email"; old = $oldEmail; new = $newEmail }
            @{ name = "Slack"; tag = "slack"; old = $oldSlack; new = $newSlack }
        )

        #Write-Host "[$username]:  $oldEmail, $oldSlack"

        foreach ($c in $changes) {
            if ($c.new) {
                #Write-Host "Applying $($c.name) with $($c.new)"
                if ($c.old) {
                    if ($OverwriteTags) {
                        Write-Warning "Overwriting $($c.name) Tag for [$username]:  [$($c.old)] => [$($c.new)]"
                        if (-not $WhatIf) {
                            Add-IAMUserTag -UserName $username -Tag @{ Key = $c.tag; Value = $c.new } @awsCreds
                        }
                    }
                    else {
                        Write-Warning "SKIPPING overwrite of existing $($c.name) Tag for [$username]:  [$($c.old)]"
                    }
                }
                else {
                    Write-Warning "Adding $($c.name) Tag for [$username]:  [$($c.new)]"
                    if (-not $WhatIf) {
                        Add-IAMUserTag -UserName $username -Tag @{ Key = $c.tag; Value = $c.new } @awsCreds
                    }
                }
            }
        }
    }
}