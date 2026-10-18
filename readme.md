# WinSymlinkFinder

Application de bureau moderne développée en **C# / .NET 8** et **WinUI 3 (Windows App SDK)** permettant de détecter, lister, inspecter et filtrer les liens symboliques (fichiers et répertoires) ainsi que les points de jonction NTFS sous Windows.

---

## 📌 À propos de la conservation des liens symboliques sous Windows

> **Windows conserve-t-il une liste des liens symboliques dans le registre ou ailleurs ?**
>
> **Non.** Windows et le système de fichiers NTFS ne maintiennent **aucun registre centralisé**, base de données ou clé de registre indexant l'ensemble des liens symboliques créés sur la machine.
>
> Les liens symboliques et les points de jonction sont stockés directement dans les métadonnées de chaque entrée de la table des fichiers maîtres (MFT - *Master File Table*) de NTFS sous forme de **points d'analyse** (*Reparse Points*).
> Chaque point d'analyse dispose d'un attribut système (`FILE_ATTRIBUTE_REPARSE_POINT`) et d'un tag spécifique :
> - `IO_REPARSE_TAG_SYMLINK` (`0xA000000C`) pour les liens symboliques (fichiers ou dossiers).
> - `IO_REPARSE_TAG_MOUNT_POINT` (`0xA0000003`) pour les jonctions NTFS.
>
> Pour découvrir ces liens, l'application scanne les disques ou dossiers cibles via les API système natives et extrait le chemin cible grâce au tampon d'analyse (*Reparse Data Buffer*).

---

## ✨ Fonctionnalités

- **Interface Fluent Design WinUI 3** : respect des standards graphiques Windows 11 (Mica, navigation NavigationView, icônes Segoe Fluent).
- **Scan ciblé ou global** : sélection d'un lecteur complet ou d'un dossier personnalisé.
- **Détection complète** :
  - Liens symboliques de fichiers (`Symlink File`)
  - Liens symboliques de dossiers (`Symlink Directory`)
  - Jonctions de répertoires NTFS (`Junction`)
- **Inspection détaillée** :
  - Nom et chemin d'accès complet du lien.
  - Cible réelle pointée (chemin absolu ou relatif).
  - Statut du lien : valide ou orphelin (cible introuvable / cassée).
  - Date de création et modification.
- **Actions rapides** :
  - Ouvrir l'emplacement dans l'Explorateur Windows.
  - Copier le chemin source ou cible dans le presse-papiers.
  - Exporter les résultats au format CSV / JSON.

---

## 🛠️ Prérequis et Compilation

- **Système d'exploitation** : Windows 10 (version 1809 ou supérieure) / Windows 11.
- **SDK .NET** : .NET 8.0 SDK ou version ultérieure.
- **Visual Studio 2022 / Build Tools** avec la charge de travail *Développement d'applications de bureau .NET* et les composants du *Windows App SDK*.

### Compilation en ligne de commande :
```powershell
dotnet build -c Release
```

### Génération d'un exécutable unique autonome (Single-File) :
Pour produire un binaire `.exe` autonome (contenant le runtime .NET 8, le Windows App SDK et WinUI 3, exécutable directement sans prérequis) :
```powershell
dotnet publish src/WinSymlinkFinder/WinSymlinkFinder.csproj -c Release -p:PublishProfile=win-x64-singlefile
```
L'exécutable autonome est généré dans :
`src/WinSymlinkFinder/bin/publish/singlefile/WinSymlinkFinder.exe`

---

## 📜 Conventions de commit

Le projet suit la convention [Conventional Commits](https://www.conventionalcommits.org/) :
- `feat:` nouvelle fonctionnalité
- `fix:` correction de bogue
- `refactor:` modification du code sans altération du comportement fonctionnel
- `docs:` documentation
- `style:` mise en page, formatage
- `chore:` tâches de maintenance et de configuration de build
