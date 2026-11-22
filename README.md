# KORVARYS Hardware Doctor

Diagnóstico local e seguro de hardware para Windows, começando pela placa de vídeo.

## Estado

Protótipo funcional de coleta: inventário da GPU, driver, DirectX, memória, sistema e eventos recentes relacionados a display/driver. Nenhum dado é enviado para a internet.

## Uso

Abra o PowerShell como usuário normal e execute:

```powershell
Set-ExecutionPolicy -Scope Process Bypass
.\src\Get-HardwareDoctorReport.ps1 -OutputPath .\report.json
```

O relatório é salvo localmente. O script é somente leitura e não instala, remove ou altera drivers.

## Roadmap

- classificar evidências de falha de driver, temperatura, energia e hardware;
- adicionar testes controlados de GPU/VRAM;
- interface desktop;
- suporte a CPU, memória e armazenamento;
- ambiente inicializável de diagnóstico.

## Segurança

Não envie relatórios públicos sem revisar nomes de usuário, caminhos e identificadores do equipamento.

Licença MIT.
