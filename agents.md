# Directives et Guide pour Agents d'Assistance IA (agents.md)

Ce document récapitule les principes directeurs, l'architecture et les règles de contribution pour tout agent IA travaillant sur ce projet.

---

## 🎯 Objectif du Projet

Développer une application de bureau Windows moderne (WinUI 3 / Windows App SDK) pour répertorier, filtrer et inspecter les liens symboliques et points de jonction du système de fichiers NTFS.

---

## 🏗️ Architecture et Choix Techniques

1. **Framework d'interface** :
   - WinUI 3 (Windows App SDK) sous .NET 8.
   - Respect strict des directives d'interface Windows (Fluent Design, navigation latérale `NavigationView`, animations subtiles, thème sombre/clair dynamique).
   - Séparation MVVM (Model - View - ViewModel) avec CommunityToolkit.Mvvm si nécessaire.

2. **Moteur de scan NTFS et Détection** :
   - Scanner asynchrone non-bloquant (`IAsyncEnumerable` ou flux d'événements) permettant de mettre à jour l'interface en temps réel sans geler l'UI.
   - Utilisation des API `System.IO` modernes (.NET 8 possède `FileSystemInfo.LinkTarget` et `ResolveLinkTarget`) combinées avec P/Invoke Win32 si nécessaire (`FSCTL_GET_REPARSE_POINT`, `DeviceIoControl`) pour extraire de manière fiable les cibles et types de reparse points.
   - Gestion robuste des erreurs (`UnauthorizedAccessException`, `PathTooLongException`, cycles infinis de liens).

3. **Conventions Git & Commits** :
   - Respect strict de **Conventional Commits** :
     - `feat:` pour les ajouts de fonctionnalités.
     - `fix:` pour les corrections de bugs.
     - `refactor:` pour les réorganisations de code.
     - `docs:` pour la documentation.
     - `chore:` pour les dépendances ou la configuration.
   - Commits atomiques après chaque étape ou modification validée.

---

## ⚠️ Points d'Attention Particuliers

- **Absence de registre Windows centralisé** : Windows n'indexe pas les liens symboliques dans le registre. Tout résultat provient d'un parcours direct du système de fichiers ou de l'analyse MFT.
- **Droits d'accès** : Certains dossiers système (`System Volume Information`, etc.) déclenchent des `UnauthorizedAccessException`. Le scanner doit les ignorer proprement sans interrompre le processus.
- **Boucles récursives** : Un lien symbolique ou une jonction pointant vers un dossier parent ou un ancêtre peut créer une boucle infinie. Le scanner doit maintenir un ensemble des chemins réels visités ou ne pas suivre les jonctions récursivement pour lister les liens.
