# SARGON Defender valdymo programa. Reikia Windows PowerShell 5.1 ir Windows 10/11.
# Microsoft Defender lieka apsaugos varikliu; šis įrankis jo nustatymų neišjungia ir nekeičia.
# ClamAV, jei įdiegtas per SARGON, naudojamas tik pasirinktų failų papildomai patikrai.

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
[System.Windows.Forms.Application]::EnableVisualStyles()

$script:activeJob = $null
$script:busy = $false

function Add-Log([string]$Text) {
    $stamp = (Get-Date).ToString('yyyy-MM-dd HH:mm:ss')
    $logBox.AppendText("[$stamp] $Text`r`n`r`n")
    $logBox.SelectionStart = $logBox.TextLength
    $logBox.ScrollToCaret()
}

function Format-Age($Value) {
    if ($null -eq $Value -or [uint64]$Value -ge 65535) { return 'nežinoma' }
    return [string]$Value
}

function Get-DefenderStatusText {
    $providers = try { @(Get-CimInstance -Namespace root/SecurityCenter2 -ClassName AntiVirusProduct -ErrorAction Stop | ForEach-Object { $_.displayName }) } catch { @() }
    $providerText = if ($providers.Count) { $providers -join ', ' } else { 'Nepavyko nustatyti; tikrinkite Windows saugoje' }
    $otherProviders = @($providers | Where-Object { $_ -and $_ -notmatch 'Microsoft Defender|Windows Defender' })
    Import-Module Defender -ErrorAction Stop
    $s = Get-MpComputerStatus -ErrorAction Stop
    $provider = if ($s.AntivirusEnabled) { 'Defender aktyvus kaip antivirusinė' } else { 'Defender nepraneša esąs aktyvus antivirusinis teikėjas' }
    $realtime = if ($s.RealTimeProtectionEnabled) { 'ĮJUNGTA' } else { 'IŠJUNGTA / nepatvirtinta' }
    $providerHint = if ($otherProviders.Count -gt 0 -and -not $s.AntivirusEnabled) {
        'Windows sauga rodo kitą antivirusinę (' + ($otherProviders -join ', ') + '). Tokiu atveju Defender gali nebūti pagrindinis aktyvus teikėjas. Tai savaime nereiškia, kad kompiuteris neapsaugotas; tikrinkite Windows saugos skydelyje, kuri apsauga aktyvi.'
    } elseif (-not $s.AntivirusEnabled) {
        'Defender nepraneša esąs pagrindinis aktyvus antivirusinis teikėjas. Prieš šalindami kitą antivirusinę, patikrinkite Windows saugą.'
    } else {
        'Windows sauga ir Defender būsena turi būti tikrinamos kartu; SARGON pats neįjungia apsaugos.'
    }
    @(
        'WINDOWS SAUGOJE REGISTRUOTI ANTIVIRUSINIAI PRODUKTAI'
        $providerText
        $providerHint
        'Aktyvią apsaugą patikrinkite Windows saugoje.'
        ''
        'MICROSOFT DEFENDER BŪSENA'
        $provider
        "Apsauga realiuoju laiku: $realtime"
        "Apsaugos bazės: $($s.AntivirusSignatureVersion)"
        "Bazių atnaujinimo laikas: $($s.AntivirusSignatureLastUpdated)"
        "Bazių amžius dienomis: $(Format-Age $s.AntivirusSignatureAge)"
        "Paskutinė greitoji patikra prieš (d.): $(Format-Age $s.QuickScanAge)"
        "Paskutinė pilna patikra prieš (d.): $(Format-Age $s.FullScanAge)"
        "Variklio režimas: $($s.AMRunningMode)"
        ''
        'Šią informaciją pateikė Windows Defender. Tai nėra garantija, kad grėsmių nėra.'
    ) -join "`r`n"
}

function Refresh-Status {
    try {
        $statusBox.Text = Get-DefenderStatusText
        Add-Log 'Defender būsena nuskaityta.'
    } catch {
        $statusBox.Text = "Defender būsenos nepavyko nuskaityti.`r`n$($_.Exception.Message)`r`n`r`nPatikrinkite Windows sauga → Apsauga nuo virusų ir grėsmių."
        Add-Log "KLAIDA skaitant Defender būseną: $($_.Exception.Message)"
    }
}

function Start-DefenderJob([string]$Action, [string]$Path = '') {
    if ($script:busy) { return }
    $script:busy = $true
    $progressLabel.Text = 'Vykdoma: ' + $Action + '…'
    $logBox.AppendText("[$((Get-Date).ToString('yyyy-MM-dd HH:mm:ss'))] Pradėta: $Action`r`n")
    try {
        $script:activeJob = Start-Job -ArgumentList $Action, $Path -ScriptBlock {
            param($Action, $Path)
            $ErrorActionPreference = 'Stop'
            if ($Action -ne 'ClamFile') {
                Import-Module Defender -ErrorAction Stop
                $s = Get-MpComputerStatus -ErrorAction Stop
                if (-not $s.AMServiceEnabled -or -not $s.AntivirusEnabled) {
                    throw 'Defender veiksmų paleisti nepavyko: jis nepraneša, kad yra aktyvus. Atverkite Windows saugą ir patikrinkite aktyvų teikėją. Esamos apsaugos neišjunkite.'
                }
            }
            switch ($Action) {
                'Update' {
                    Update-MpSignature -ErrorAction Stop
                    $s = Get-MpComputerStatus -ErrorAction Stop
                    "Defender apsaugos bazių atnaujinimo komanda baigė darbą.`r`nVersija: $($s.AntivirusSignatureVersion)`r`nAtnaujinta: $($s.AntivirusSignatureLastUpdated)`r`nBazių amžius dienomis: $($s.AntivirusSignatureAge)"
                }
                'Quick' {
                    Start-MpScan -ScanType QuickScan -ErrorAction Stop
                    "Defender greitosios patikros komanda įvykdyta $((Get-Date).ToString('g')).`r`nRadinius ir veiksmus peržiūrėkite Windows sauga → Apsaugos istorija."
                }
                'Full' {
                    Start-MpScan -ScanType FullScan -ErrorAction Stop
                    "Defender viso kompiuterio patikros komanda įvykdyta $((Get-Date).ToString('g')).`r`nPatikra gali tęstis fone. Radinius ir veiksmus peržiūrėkite Windows sauga → Apsaugos istorija."
                }
                'Custom' {
                    if ([string]::IsNullOrWhiteSpace($Path) -or -not (Test-Path -LiteralPath $Path)) { throw 'Pasirinktas failas nerastas.' }
                    Start-MpScan -ScanType CustomScan -ScanPath $Path -ErrorAction Stop
                    "Defender failo patikros komanda įvykdyta.`r`nFailas: $Path`r`nRadinius peržiūrėkite Windows sauga → Apsaugos istorija."
                }
                'History' {
                    $items = @(Get-MpThreatDetection -ErrorAction Stop | Sort-Object InitialDetectionTime -Descending | Select-Object -First 30 InitialDetectionTime, ThreatID, ThreatStatusID, ActionSuccess, Resources)
                    if ($items.Count -eq 0) { 'Defender nepateikė grėsmių istorijos įrašų.' }
                    else { $items | Format-List * | Out-String -Width 220 }
                }
                'ClamFile' {
                    if ([string]::IsNullOrWhiteSpace($Path) -or -not (Test-Path -LiteralPath $Path -PathType Leaf)) { throw 'Pasirinktas failas nerastas.' }
                    $root = Join-Path $env:ProgramFiles 'SARGONService\engine\clamav-1.4.6.win.x64'
                    $scanner = Join-Path $root 'clamscan.exe'
                    $db = Join-Path $env:ProgramData 'SARGONService\Databases'
                    if (-not (Test-Path -LiteralPath $scanner -PathType Leaf) -or -not (Test-Path -LiteralPath $db -PathType Container)) { throw 'Papildomas ClamAV variklis arba jo bazės nerastos. Microsoft Defender veikia atskirai.' }
                    $output = & $scanner "--database=$db" --no-summary $Path 2>&1 | Out-String -Width 220
                    $code = $LASTEXITCODE
                    if ($code -eq 0) { "ClamAV grąžino kodą 0: žymų neaptikta. Patikrinkite išvestį dėl perspėjimų; tai nėra saugumo garantija.`r`n$output" }
                    elseif ($code -eq 1) { "ClamAV pateikė radinį. Patikrinkite vardą ir veiksmus SARGON / Windows saugumo aplinkoje; failas automatiškai neištrintas.`r`n$output" }
                    else { throw "ClamAV grąžino klaidos kodą $code.`r`n$output" }
                }
            }
        }
        $script:pollTimer.Start()
    } catch {
        $script:busy = $false
        $progressLabel.Text = 'Paruošta'
        Add-Log "Nepavyko pradėti veiksmo: $($_.Exception.Message)"
    }
}

$form = New-Object System.Windows.Forms.Form
$form.Text = 'SARGON Security — Defender valdymas'
$form.Size = New-Object System.Drawing.Size(1000, 720)
$form.MinimumSize = New-Object System.Drawing.Size(820, 600)
$form.StartPosition = 'CenterScreen'
$form.BackColor = [System.Drawing.Color]::FromArgb(12, 22, 36)
$form.ForeColor = [System.Drawing.Color]::FromArgb(230, 240, 248)
$form.Font = New-Object System.Drawing.Font('Segoe UI', 10)

$title = New-Object System.Windows.Forms.Label
$title.Text = 'SARGON Security  |  Saugumo valdymas'
$title.Font = New-Object System.Drawing.Font('Segoe UI', 18, [System.Drawing.FontStyle]::Bold)
$title.ForeColor = [System.Drawing.Color]::FromArgb(104, 221, 195)
$title.Location = New-Object System.Drawing.Point(20, 18)
$title.Size = New-Object System.Drawing.Size(920, 38)
$form.Controls.Add($title)

$notice = New-Object System.Windows.Forms.Label
$notice.Text = 'Nemokamas pagrindinis pakaitalas: Windows integruotas Microsoft Defender. SARGON tik jį valdo ir tikrina būseną; ClamAV — papildomas skaitytuvas.'
$notice.Location = New-Object System.Drawing.Point(22, 61)
$notice.Size = New-Object System.Drawing.Size(930, 42)
$notice.ForeColor = [System.Drawing.Color]::FromArgb(255, 211, 103)
$form.Controls.Add($notice)

$buttons = New-Object System.Windows.Forms.FlowLayoutPanel
$buttons.Location = New-Object System.Drawing.Point(20, 108)
$buttons.Size = New-Object System.Drawing.Size(230, 510)
$buttons.FlowDirection = 'TopDown'
$buttons.WrapContents = $false
$buttons.AutoScroll = $true
$buttons.BackColor = $form.BackColor
$form.Controls.Add($buttons)

function Add-ActionButton([string]$Text, [scriptblock]$Click) {
    $button = New-Object System.Windows.Forms.Button
    $button.Text = $Text
    $button.Width = 210
    $button.Height = 48
    $button.Margin = New-Object System.Windows.Forms.Padding(4, 4, 4, 8)
    $button.BackColor = [System.Drawing.Color]::FromArgb(31, 57, 80)
    $button.ForeColor = [System.Drawing.Color]::White
    $button.FlatStyle = 'Flat'
    $button.Add_Click($Click)
    $buttons.Controls.Add($button)
}

Add-ActionButton 'Atnaujinti būseną' { Refresh-Status }
Add-ActionButton 'Atnaujinti Defender bazes' { Start-DefenderJob 'Update' }
Add-ActionButton 'Greitoji Defender patikra' { Start-DefenderJob 'Quick' }
Add-ActionButton 'Viso kompiuterio patikra' {
    if ([System.Windows.Forms.MessageBox]::Show('Defender tikrins visą kompiuterį; tai gali užtrukti. Pradėti?', 'SARGON Security', [System.Windows.Forms.MessageBoxButtons]::YesNo, [System.Windows.Forms.MessageBoxIcon]::Question) -eq [System.Windows.Forms.DialogResult]::Yes) { Start-DefenderJob 'Full' }
}
Add-ActionButton 'Patikrinti pasirinktą failą' {
    $dialog = New-Object System.Windows.Forms.OpenFileDialog
    $dialog.Title = 'Pasirinkite failą Microsoft Defender patikrai'
    if ($dialog.ShowDialog($form) -eq 'OK') { Start-DefenderJob 'Custom' $dialog.FileName }
}
Add-ActionButton 'Defender radinių istorija' { Start-DefenderJob 'History' }
Add-ActionButton 'Pereiti nuo McAfee prie Defender' {
    $status = 'Defender būsenos patikrinti nepavyko. Prieš šalindami McAfee patikrinkite Windows saugą.'
    try {
        Import-Module Defender -ErrorAction Stop
        $s = Get-MpComputerStatus -ErrorAction Stop
        if ($s.AMServiceEnabled -and $s.AntivirusEnabled -and $s.RealTimeProtectionEnabled) {
            $status = 'DABAR: Defender antivirusinė ir apsauga realiuoju laiku įjungtos. Patvirtinkite aktyvų teikėją Windows saugos programoje.'
        } else {
            $status = 'DABAR: Defender dar nepatvirtino veikiančios antivirusinės ir apsaugos realiuoju laiku. McAfee gali tebebūti aktyvus.'
        }
    } catch {
        $status = 'DABAR: Defender būsenos patikrinti nepavyko. ' + $_.Exception.Message
    }
    $message = "Nemokamas antivirusinis pakaitalas yra Windows integruotas Microsoft Defender; SARGON jo nepakeičia.`r`n`r`n$status`r`n`r`nPerėjimas: atverkite Windows Programas, pašalinkite McAfee tik kai esate pasiruošę, tada grįžkite į SARGON ir spauskite Atnaujinti būseną. Jei prašoma, paleiskite kompiuterį iš naujo. Perėjimas baigtas tik kai Defender antivirusinė ir apsauga realiuoju laiku įjungtos, o Windows sauga rodo Microsoft Defender kaip aktyvų teikėją. Jei ne, iš naujo nešalinkite kitų apsaugos priemonių ir atverkite Windows saugą."
    $choice = [System.Windows.Forms.MessageBox]::Show($message + "`r`n`r`nAtverti Windows Programų nustatymus?", 'Perėjimas prie nemokamo Microsoft Defender', [System.Windows.Forms.MessageBoxButtons]::YesNo, [System.Windows.Forms.MessageBoxIcon]::Information)
    if ($choice -eq [System.Windows.Forms.DialogResult]::Yes) { Start-Process 'ms-settings:appsfeatures' }
}
Add-ActionButton 'Papildoma ClamAV failo patikra' {
    $dialog = New-Object System.Windows.Forms.OpenFileDialog
    $dialog.Title = 'Pasirinkite failą papildomai ClamAV patikrai'
    if ($dialog.ShowDialog($form) -eq 'OK') { Start-DefenderJob 'ClamFile' $dialog.FileName }
}
Add-ActionButton 'Atverti Windows saugumą' {
    try { Start-Process 'windowsdefender://threat' } catch { Start-Process 'ms-settings:windowsdefender' }
}

$statusBox = New-Object System.Windows.Forms.TextBox
$statusBox.Location = New-Object System.Drawing.Point(270, 108)
$statusBox.Size = New-Object System.Drawing.Size(690, 210)
$statusBox.Multiline = $true
$statusBox.ReadOnly = $true
$statusBox.ScrollBars = 'Vertical'
$statusBox.BackColor = [System.Drawing.Color]::FromArgb(23, 36, 56)
$statusBox.ForeColor = $form.ForeColor
$statusBox.Font = New-Object System.Drawing.Font('Consolas', 10)
$form.Controls.Add($statusBox)

$logBox = New-Object System.Windows.Forms.TextBox
$logBox.Location = New-Object System.Drawing.Point(270, 330)
$logBox.Size = New-Object System.Drawing.Size(690, 290)
$logBox.Multiline = $true
$logBox.ReadOnly = $true
$logBox.ScrollBars = 'Vertical'
$logBox.BackColor = [System.Drawing.Color]::FromArgb(23, 36, 56)
$logBox.ForeColor = $form.ForeColor
$logBox.Font = New-Object System.Drawing.Font('Consolas', 9)
$form.Controls.Add($logBox)

$progressLabel = New-Object System.Windows.Forms.Label
$progressLabel.Text = 'Paruošta'
$progressLabel.Location = New-Object System.Drawing.Point(270, 630)
$progressLabel.Size = New-Object System.Drawing.Size(690, 26)
$form.Controls.Add($progressLabel)

$script:pollTimer = New-Object System.Windows.Forms.Timer
$script:pollTimer.Interval = 700
$script:pollTimer.Add_Tick({
    if (-not $script:activeJob) { $script:pollTimer.Stop(); return }
    $job = Get-Job -Id $script:activeJob.Id -ErrorAction SilentlyContinue
    if ($null -eq $job -or $job.State -in @('Completed', 'Failed', 'Stopped')) {
        $script:pollTimer.Stop()
        if ($job) {
            $result = Receive-Job -Job $job -ErrorAction Continue 2>&1 | Out-String -Width 220
            if ($job.State -eq 'Completed') { Add-Log $result.Trim() }
            else { Add-Log ("KLAIDA: " + $result.Trim()) }
            Remove-Job -Job $job -Force -ErrorAction SilentlyContinue
        } else { Add-Log 'Veiksmo būsena prarasta; patikrinkite Windows saugą.' }
        $script:activeJob = $null
        $script:busy = $false
        $progressLabel.Text = 'Paruošta'
        Refresh-Status
    }
})

$form.Add_Shown({ Refresh-Status })
$form.Add_FormClosed({
    if ($script:activeJob) { Remove-Job -Job $script:activeJob -Force -ErrorAction SilentlyContinue }
})
[void]$form.ShowDialog()
