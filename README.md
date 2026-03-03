<div align="center">

# 🟣 CalyRecall 🟣 

**Automação de Backup e Restauração Inteligente para Steam (Millennium)**

[![Millennium](https://img.shields.io/badge/Millennium-Compatible-8b5cf6?style=for-the-badge&logo=steam)](https://steambrew.app/)
[![Python](https://img.shields.io/badge/Backend-Python-ffe800?style=for-the-badge&logo=python&logoColor=black)](https://www.python.org/)
[![Discord](https://img.shields.io/badge/Community-Discord-5865F2?style=for-the-badge&logo=discord&logoColor=white)](https://discord.gg/DQYxmFaywK)
[![Status](https://img.shields.io/badge/Status-Active-success?style=for-the-badge)]()
[![License](https://img.shields.io/badge/License-CSAL-red?style=for-the-badge)](LICENSE)

<p align="center">
  <img src="https://media.giphy.com/media/v1.Y2lkPTc5MGI3NjExM3BxdGp6Z3V4ZnV4ZnV4ZnV4ZnV4ZnV4ZnV4ZnV4ZnV4ZnV4eiZlcD12MV9pbnRlcm5hbF9naWZfYnlfaWQmY3Q9Zw/LMcB8XjhG7ck/giphy.gif" width="100%" height="4" alt="divider">
</p>

<h3>Proteja seu legado. Viaje no tempo.</h3>

<p align="left">
O <strong>CalyRecall</strong> é um plugin de segurança silencioso. Ele monitora sua sessão de jogo em tempo real. No momento em que você fecha um jogo, o protocolo <em>Recall</em> é ativado, criando um snapshot instantâneo dos seus dados mais valiosos.
<br><br>
Agora com o novo sistema de <strong>Restore e Portabilidade</strong>, você tem controle absoluto sobre os seus saves, podendo reverter para qualquer ponto da história com apenas dois cliques ou exportar tudo com segurança. Nunca mais perca um progresso, uma configuração ou um status de plugin.
</p>

</div>

---

## ⚡ Funcionalidades

| Recurso | Descrição |
| :--- | :--- |
| 🎮 **Game Awareness** | Identifica automaticamente qual jogo foi fechado, exibindo o **Nome Real** e a **Capa Oficial** na lista de backups. |
| 🕵️ **Monitoramento Passivo** | Detecta automaticamente o encerramento de processos de jogos (AppID). Zero impacto na performance. |
| ⌨️ **Quick-Save Global** | Permite gravar um atalho (bind) para forçar um backup instantâneo a qualquer momento, mesmo com a Steam minimizada. |
| 📦 **Backup Cirúrgico** | Salva apenas o que importa (userdata, stats, cache, configs), ignorando o "lixo" temporário. |
| 🗜️ **Portabilidade (.zip)** | Exporte ou Importe todos os seus backups compactados de forma segura em um único arquivo. |
| 📊 **Dashboard Integrado** | Pílula de estatísticas em tempo real mostrando o peso dos saves, contagem de backups e espaço livre no SSD/HD. |
| 🔄 **Time Travel (Restore)** | Restaure backups antigos instantaneamente através de uma interface visual integrada. |
| 🎚️ **Controle de Modos** | Alterne de forma fluida entre os modos **Automático**, **Semi-Auto** (com aprovação via modal) e **Manual**. |
| 🔔 **Notificações Nativas** | Feedback visual discreto via Windows Toast ao concluir operações. |

---

## 🕰️ O Ecossistema CalyRecall

O CalyRecall agora possui um verdadeiro painel de controle embutido na sua Steam. Veja como dominar a ferramenta:

### 1. O Botão de Acesso
No canto inferior direito da sua Steam, procure pelo **Botão Roxo com Ícone de Relógio**. Ele é o seu portal para o protocolo.

<div align="center">
  <img src="https://i.imgur.com/gReSM17.png" alt="Botão CalyRecall" width="35%">
</div>

### 2. Gerenciamento Visual (Restore)
Ao clicar, uma lista com todos os seus backups aparecerá, acompanhada das capas oficiais dos jogos!
* **Restaurar:** Clique no botão grande para voltar no tempo.
* **Renomear (✏️):** Dê apelidos aos seus backups (ex: "Antes do Boss Final").
* **Deletar (🗑️):** Remova snapshots antigos.

<div align="center">
  <img src="https://i.imgur.com/VyUo3gR.png" alt="Menu de Restore" width="50%">
</div>

### 3. Pílula de Estatísticas em Tempo Real
Para você ter controle total do seu hardware, o painel agora exibe uma pílula informativa inteligente que lê os dados do seu computador.
* **Peso Total:** Descubra quantos GB/MB seus saves estão ocupando.
* **Contador:** O número exato de snapshots ativos.
* **Armazenamento:** Monitoramento ao vivo do espaço livre do seu SSD/HDD.

<div align="center">
  <img src="https://i.imgur.com/ZumMQRk.png" width="50%">
</div>

### 4. Controle de Modos e Quick-Save
Na aba **Configurações**, você encontra a nova barra deslizante para selecionar exatamente como o protocolo deve agir:

* **Automático:** Criado de forma silenciosa e invisível ao fechar o jogo.
* **Semi-Auto:** Exibe um modal perguntando se você deseja salvar aquele momento.
* **Manual (Quick-Save):** Você define uma *bind* de teclado e faz o backup instantaneamente durante a gameplay sem precisar abrir a Steam.

<div align="center">
  <img src="https://i.imgur.com/XNr34R7.png" alt="Seleção de Modos" width="50%">
</div>

### 5. Portabilidade Segura (.zip)
Vai formatar o PC ou jogar em outra máquina? A nova função de Portabilidade permite que você compacte toda a sua linha do tempo de backups em um único arquivo `.zip` com um clique, e faça a importação de volta com a mesma facilidade, acompanhado de barras de progresso visuais.

<div align="center">
  <img src="https://i.imgur.com/tv7PENd.png" width="50%">
</div>

<div align="center">
  <img src="https://i.imgur.com/huhnr1i.png" width="50%">
</div>

---

## 🛡️ O Protocolo de Segurança (Backup Targets)

O **CalyRecall** foi configurado para "congelar" o estado das seguintes pastas críticas de forma cirúrgica:

> **📂 1. Userdata (`/userdata`)**
> * Contém todos os seus saves locais, configurações de controle e preferências de nuvem.
>
> **📊 2. Estatísticas (`/appcache/stats`)**
> * Preserva os arquivos de métricas e estatísticas dos seus jogos.
>
> **📦 3. Depot Cache (`/depotcache`)**
> * Arquivos de manifesto e cache de download cruciais para a integridade dos jogos.
>
> **🔌 4. Configurações de Plugins (`/config/stplug-in`)**
> * Backup específico para configurações de plugins injetados na Steam.

---

## 🚀 Como Instalar

⚠️ **Pré-requisito:** Tenha o [Millennium](https://steambrew.app/) instalado.

### ⚡ Método Recomendado (Instalador Oficial)
A forma mais fácil, bonita e segura de instalar.

1. Vá até a aba **Releases** aqui no GitHub.
2. Baixe o arquivo `calyrecall-installer.exe`.
3. Execute o arquivo.
4. Siga os passos na tela e configure sua instalação.
   *(O instalador fechará a Steam automaticamente para garantir uma injeção limpa).*

<div align="center">
  <img src="https://i.imgur.com/ihobPo8.png" alt="Preview Tela Inicial" width="45%">
  <img src="https://i.imgur.com/dOWCLwh.png" alt="Preview Instalação Personalizada" width="45%">
</div>

### ⚙️ Instalação Personalizada
O instalador do CalyRecall é inteligente e permite flexibilidade total:

* **Steam em outro local?** O instalador tenta detectar sua Steam automaticamente. Caso você a tenha em um diretório diferente (ex: `D:\Games\Steam`), você pode selecionar a pasta manualmente.
* **Pasta de Backups Externa:** Você tem pouco espaço no disco principal? Durante a instalação, você pode escolher uma **Pasta de Backup Personalizada** (como um HD externo ou pasta do Google Drive) e o protocolo irá salvar os dados diretamente lá.

---

### 🛠️ Método Manual (Avançado)

Caso prefira extrair os arquivos manualmente:

1. Baixe a última versão do código fonte (ZIP).
2. Extraia a pasta `CalyRecall` para dentro do diretório de plugins:
   `.../Steam/plugins/CalyRecall`
3. Reinicie a Steam.

---

## 📂 Onde ficam meus backups?

Se você usou a instalação padrão, seus snapshots ficam seguros dentro da própria pasta do plugin:

```text
Steam/
└── plugins/
    └── CalyRecall/
        └── backups/
            ├── CalyBackup-2026-01-24_14-30-00/
            ├── CalyBackup-2026-01-24_18-45-12/
            └── ...
