Describe 'Get-HardwareDoctorReport' {
    It 'contém um schema versionado e não expõe conteúdo fictício' {
        $script = Get-Content (Join-Path $PSScriptRoot '..\src\Get-HardwareDoctorReport.ps1') -Raw
        $script | Should -Match 'schemaVersion = 1'
        $script | Should -Not -Match 'example|fake|mock|Lorem'
    }

    It 'não executa instalação ou remoção de driver' {
        $script = Get-Content (Join-Path $PSScriptRoot '..\src\Get-HardwareDoctorReport.ps1') -Raw
        $script | Should -Not -Match 'pnputil|Install-Package|Remove-Item.*driver|devcon'
    }
}
