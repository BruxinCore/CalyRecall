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
Agora com o novo sistema de <strong>Restore</strong>, você pode reverter para qualquer ponto da história com apenas dois cliques. Nunca mais perca um save, uma configuração ou um status de plugin.
</p>

</div>

---

## ⚡ Funcionalidades

| Recurso | Descrição |
| :--- | :--- |
| 🎮 **Game Awareness** |Identifica automaticamente qual jogo foi fechado, exibindo o **Nome Real** e a **Capa Oficial** na lista de backups. |
| 🕵️ **Monitoramento Passivo** | Detecta automaticamente o encerramento de processos de jogos (AppID). Zero impacto na performance. |
| 📦 **Backup Cirúrgico** | Salva apenas o que importa (userdata, stats, cache, configs), ignorando o "lixo" temporário. |
| 🔄 **Time Travel (Restore)** | Restaure backups antigos instantaneamente através de uma interface visual integrada. |
| ✏️ **Gerenciamento Total** | Renomeie backups (ex: "Antes do Boss") ou delete snapshots antigos direto na interface. |
| 🔔 **Notificações Nativas** | Feedback visual discreto via Windows Toast ao concluir operações. |
| 🗃️ **Histórico Organizado** | Cria pastas timestamped para você voltar no tempo quando quiser. |
| ⚙️ **Semi-Automático** | Exibe um modal de confirmação ao fechar o jogo para você decidir se deseja salvar. Nada é gravado sem sua aprovação. Controle na aba Configurações. |

---

## 🕰️ Como usar o Restore

O CalyRecall agora possui uma interface visual dedicada. Veja como é simples voltar no tempo:

### 1. O Botão de Acesso
No canto inferior direito da sua Steam, procure pelo **Botão Roxo com Ícone de Relógio**. Ele é o seu portal para os backups.

<div align="center">
  <img src="https://i.imgur.com/gReSM17.png" alt="Botão CalyRecall" width="35%">
</div>

### 2. Gerenciamento Visual
Ao clicar, uma lista com todos os seus backups aparecerá, agora com os ícones dos jogos!
* **Restaurar:** Clique no botão grande para voltar no tempo.
* **Renomear (✏️):** Dê apelidos aos seus backups para lembrar de momentos importantes.
* **Deletar (🗑️):** Remova backups que não precisa mais.

<div align="center">
  <img src="https://i.imgur.com/w3NpTcM.png" alt="Menu de Restore" width="50%">
</div>

### 3. Confirmação Visual
Pronto! O CalyRecall fará a substituição cirúrgica dos arquivos e te avisará quando estiver tudo seguro.

<div align="center">
  <img src="https://i.imgur.com/dD5YAs7.png" alt="Sucesso" width="50%">
</div>

### 4. Semi-Automático (Opcional)
Ao encerrar um jogo, o CalyRecall pode exibir um modal pedindo sua confirmação antes de salvar o snapshot.
– Ignorar: fecha o modal e nenhum backup é criado  
– Salvar: cria um backup com identificação do jogo e horário

<div align="center">
  <img src="images/semiauto-modal-placeholder.png" alt="Modal de Confirmação (Semi-Automático)" width="50%">
</div>

### Configurações
Na aba Configurações do painel CalyRecall você encontra:
– Modo Semi-Automático: alterna o fluxo que exibe o modal de confirmação  
– O estado fica salvo entre sessões

---

## 🛡️ O Protocolo de Segurança (Backup Targets)

O **CalyRecall** foi configurado para "congelar" o estado das seguintes pastas críticas:

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
4. Siga os passos na tela e clique em **INSTALAR** e configure sua instalação.
   *(O instalador fechará a Steam automaticamente para garantir uma instalação limpa).*

<div align="center">
  <img src="https://i.imgur.com/ihobPo8.png" alt="Preview Tela Inicial" width="45%">
  <img src="https://i.imgur.com/dOWCLwh.png" alt="Preview Instalação Personalizada" width="45%">
</div>

### ⚙️ Instalação Personalizada
O instalador do CalyRecall é inteligente e permite flexibilidade total:

* **Steam em outro local?** O instalador tenta detectar sua Steam automaticamente. Caso você tenha instalado a Steam em um HD/SSD secundário (ex: `D:\Games\Steam`), você pode selecionar a pasta correta manualmente clicando no ícone de pasta 📂.

* **Pasta de Backups Personalizada:**
  Por padrão, os backups ficam dentro da pasta do plugin. Se você tem pouco espaço no disco principal ou prefere salvar seus saves em outro lugar (como uma nuvem ou HD/SSD externo), você pode escolher uma **Pasta de Backup Personalizada** durante a instalação.

---

### 🛠️ Método Manual (Avançado)

Caso prefira não usar o instalador:

1. Baixe a última versão do código fonte (ZIP).
2. Extraia a pasta `CalyRecall` para dentro do diretório de plugins:
   `.../Steam/plugins/CalyRecall`
3. Reinicie a Steam.

---

## 📂 Onde ficam meus backups?

Se você usou a instalação padrão, seus snapshots ficam seguros dentro da pasta do plugin:

```text
Steam/
└── plugins/
    └── CalyRecall/
        └── backups/
            ├── CalyBackup-2026-01-24_14-30-00/
            ├── CalyBackup-2026-01-24_18-45-12/
            └── ...
