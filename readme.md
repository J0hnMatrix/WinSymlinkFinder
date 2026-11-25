# WinSymlinkFinder

<p align="center">
  <img src="src/WinSymlinkFinder/Assets/StoreLogo.png" alt="WinSymlinkFinder Logo" width="96" height="96" />
</p>

<p align="center">
  <strong>Explorateur, créateur et gestionnaire de liens symboliques et points de jonction NTFS pour Windows.</strong>
</p>

<p align="center">
  <a href="https://github.com/J0hnMatrix/WinSymlinkFinder/releases/latest"><img src="https://img.shields.io/github/v/release/J0hnMatrix/WinSymlinkFinder?style=flat-square&color=0078D4" alt="Dernière version" /></a>
  <img src="https://img.shields.io/badge/.NET-11.0-512BD4?style=flat-square&logo=dotnet" alt=".NET 11" />
  <img src="https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011-0078D6?style=flat-square&logo=windows" alt="Plateforme" />
  <img src="https://img.shields.io/badge/UI-WinUI%203%20%2F%20Windows%20App%20SDK-0078D4?style=flat-square" alt="WinUI 3" />
  <a href="LICENSE"><img src="https://img.shields.io/badge/License-MIT-green?style=flat-square" alt="Licence" /></a>
</p>

---

## ⚡ Téléchargement rapide

Téléchargez directement la dernière version autonome (exécutable unique `.exe`, sans installation requise) :

👉 **[Télécharger WinSymlinkFinder sur GitHub Releases](https://github.com/J0hnMatrix/WinSymlinkFinder/releases/latest)**

*L'exécutable intègre son propre runtime .NET 11 et Windows App SDK en un fichier unique prêt à l'emploi.*

---

## 📌 À propos de la conservation des liens symboliques sous Windows

> **Windows conserve-t-il une liste des liens symboliques dans le registre ou ailleurs ?**
>
> **Non.** Windows et le système de fichiers NTFS ne maintiennent **aucun registre centralisé**, base de données ou clé de registre indexant l'ensemble des liens symboliques créés sur la machine.
>
> Les liens symboliques et les points de jonction sont stockés directement dans les métadonnées de chaque entrée de la table des fichiers maîtres (MFT - *Master File Table*) sous forme de **points d'analyse** (*Reparse Points*).
> Chaque point d'analyse dispose d'un attribut système (`FILE_ATTRIBUTE_REPARSE_POINT`) et d'un tag spécifique :
> - `IO_REPARSE_TAG_SYMLINK` (`0xA000000C`) pour les liens symboliques (fichiers ou dossiers).
> - `IO_REPARSE_TAG_MOUNT_POINT` (`0xA0000003`) pour les jonctions NTFS.
>
> Pour découvrir ces liens, **WinSymlinkFinder** scanne les répertoires du système de fichiers et interroge le tampon d'analyse (*FSCTL_GET_REPARSE_POINT*) afin d'extraire la cible et le type exact du lien.

---

## ✨ Fonctionnalités

### 🔍 1. Scanner & Inspecter les liens
- **Interface moderne WinUI 3 (Fluent Design)** : Mica backdrop, barre de titre personnalisée, icônes Segoe Fluent, thèmes clair et sombre automatiques.
- **Scan asynchrone non-bloquant** : affichage en streaming temps réel sans gel de l'interface.
- **Détection exhaustive** :
  - Liens symboliques de fichiers (`Symlink File`)
  - Liens symboliques de dossiers (`Symlink Directory`)
  - Points de jonction NTFS (`Junction`)
  - Liens physiques (`Hard Link`)
- **Diagnostic de validité** : détection immédiate des liens orphelins (cibles supprimées ou cassées).
- **Protection des composants système** : détection et affichage d'un badge jaune de protection pour les jonctions et liens internes de Windows (`Application Data`, `Documents and Settings`, etc.).
- **Filtres et recherche instantanée** : filtrage par type, par statut (cassé/valide) ou par mot-clé sur le nom, le chemin ou la cible.
- **Exportation des résultats** : export complet de la liste en **CSV** ou **JSON**.

### 🔗 2. Création de liens simplifiée
- **Flux logique guidé** : sélection d'abord de l'élément d'origine existant (source), puis de l'emplacement du nouveau lien virtuel à créer.
- **Aperçu visuel de la redirection** en temps réel.
- **Prise en charge complète** :
  - **Points de jonction NTFS** (recommandé pour les dossiers locaux, **aucun privilège administrateur requis**).
  - **Liens symboliques** de dossiers ou de fichiers (relatifs ou absolus).
  - **Hard Links** de fichiers sur la même partition NTFS.
- **Aide intelligente en cas de privilège manquant** : proposition en un clic de basculer vers une Jonction NTFS ou d'ouvrir les Paramètres Développeur Windows (`ms-settings:developers`).

### 🛡️ 3. Suppression sécurisée & Sauvegardes automatiques
- **Suppression non-destructive** : seule la référence du lien est effacée ; les dossiers et fichiers cibles d'origine restent totalement intacts.
- **Protection anti-suppression système** : interdiction stricte de supprimer les jonctions de compatibilité du système d'exploitation Windows.
- **Historique persistant des sauvegardes** : chaque lien supprimé est automatiquement sauvegardé avec l'ensemble de ses métadonnées dans `%LOCALAPPDATA%\WinSymlinkFinder\Backups` (formats **CSV** et **JSON** synchronisés).
- **Restauration en un clic** : recréez instantanément un lien supprimé par erreur à son emplacement d'origine.
- **Bouton d'accès direct au dossier local** des sauvegardes depuis l'application.

### 🐛 4. Mode Diagnostic & Debug
- Accédez à la console de logs en direct en cliquant **7 fois** sur le numéro de version dans la page **Paramètres**.

---

## 📊 Comparatif des types de liens

| Type de lien | Portée | Droits requis | Chemins relatifs | Cibles réseau (SMB) |
| :--- | :--- | :--- | :--- | :--- |
| **Jonction NTFS** | Dossiers uniquement | Utilisateur standard | Non (absolu requis) | Non (local uniquement) |
| **Symlink Dossier** | Dossiers | Mode Développeur ou Admin | Oui (même lecteur) | Oui (partage UNC `\\serveur`) |
| **Symlink Fichier** | Fichiers | Mode Développeur ou Admin | Oui (même lecteur) | Oui (partage UNC `\\serveur`) |
| **Hard Link** | Fichiers | Utilisateur standard | Non | Non (même volume NTFS) |

---

## 🛠️ Compilation depuis les sources

### Prérequis
- Windows 10 (1809+) ou Windows 11.
- [.NET 11.0 SDK](https://dotnet.microsoft.com/download/dotnet/11.0).
- Visual Studio 2022 ou les Build Tools avec les charges *Développement Desktop .NET* et le *Windows App SDK*.

### Compiler et exécuter les tests :
```powershell
dotnet test
```

### Générer l'exécutable unique autonome (Single-File) :
```powershell
dotnet publish src/WinSymlinkFinder/WinSymlinkFinder.csproj -c Release -p:PublishProfile=win-x64-singlefile
```
Le binaire autonome est produit dans :  
`src/WinSymlinkFinder/bin/publish/singlefile/WinSymlinkFinder.exe`

---

## 📜 Conventions & Licence

- Projet sous licence [MIT](LICENSE).
- Commits atomiques respectant la convention [Conventional Commits](https://www.conventionalcommits.org/).

---

## 📝 Historique des modifications

- **2026-11-25** : Passage en version **1.4.0** (.NET 11, optimisation Single-File, jonctions Win32 natives, scanner & sauvegardes optimisés).
- **2026-11-25** : Correction du crash au démarrage en mode Single-File (`Microsoft.UI.Xaml.dll` / `0xc000027b`) en définissant `MICROSOFT_WINDOWSAPPRUNTIME_BASE_DIRECTORY` dans un `[ModuleInitializer]` de `App.xaml.cs`.
- **2026-11-25** : Optimisation du moteur de scan et des services : pooling mémoire (`ArrayPool<byte>`) dans `ReparsePointHelper`, création native des jonctions NTFS via Win32 `FSCTL_SET_REPARSE_POINT` (suppression de `cmd.exe`), mise en cache du chemin Windows et throttling temporel dans `NtfsLinkScanner`, synchronisation `SemaphoreSlim` dans `LinkBackupService`, `PeriodicTimer` dans `ScannerViewModel` et mutualisation de `CsvHelper`.
- **2026-11-25** : Optimisation du profil `win-x64-singlefile` (compression single-file, `PublishTrimmed` partiel, désactivation de `PublishReadyToRun` et suppression des symboles de debug) réduisant l'exécutable autonome de **292,35 Mo à 59,58 Mo** (-80 %), ajout d'un profil `win-x64-framework-dependent`, et nettoyage de la capacité inutilisée `systemAIModels` dans `Package.appxmanifest`.
- **2026-11-25** : Migration des ViewModels vers des propriétés `partial` (`[ObservableProperty]`) et ajout d'un `JsonSerializerContext` (`AppExportJsonContext` / `AppBackupJsonContext`) pour la compatibilité Trimming / AOT WinRT.
- **2026-11-25** : Migration vers .NET 11 (`net11.0` / `net11.0-windows10.0.26100.0`) et correction de l'ordre de validation dans `LinkManagementService.CreateLinkAsync` (évite de créer le dossier parent avant la validation du mode relatif/lecteur).
