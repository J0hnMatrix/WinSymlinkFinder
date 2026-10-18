using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using WinSymlinkFinder.Services;

namespace WinSymlinkFinder.Helpers;

/// <summary>
/// Helper natif Win32 pour l'ouverture des boîtes de dialogue de sélection de dossiers et fichiers.
/// Exécute les dialogues dans un thread STA dédié avec multi-tier fallback (IFileOpenDialog -> SHBrowseForFolder).
/// </summary>
public static class NativeDialogHelper
{
    [Flags]
    public enum FOS : uint
    {
        FOS_OVERWRITEPROMPT = 0x00000002,
        FOS_STRICTFILETYPES = 0x00000004,
        FOS_NOCHANGEDIR = 0x00000008,
        FOS_PICKFOLDERS = 0x00000020,
        FOS_FORCEFILESYSTEM = 0x00000040,
        FOS_ALLNONSTORAGEITEMS = 0x00000080,
        FOS_NOVALIDATE = 0x00000100,
        FOS_ALLOWMULTISELECT = 0x00000200,
        FOS_PATHMUSTEXIST = 0x00000800,
        FOS_FILEMUSTEXIST = 0x00001000,
        FOS_CREATEPROMPT = 0x00002000,
        FOS_SHAREAWARE = 0x00004000,
        FOS_NOREADONLYRETURN = 0x00008000,
        FOS_NOTESTFILECREATE = 0x00010000,
        FOS_HIDEMRUPLACES = 0x00020000,
        FOS_HIDEPINNEDPLACES = 0x00040000,
        FOS_NODEREFERENCELINKS = 0x00100000,
        FOS_OKBUTTONNEEDSINTERACTION = 0x00200000,
        FOS_DONTADDTORECENT = 0x02000000,
        FOS_FORCESHOWHIDDEN = 0x10000000,
        FOS_DEFAULTNOMINIMODE = 0x20000000,
        FOS_FORCEPREVIEWPANEON = 0x40000000,
        FOS_SUPPORTSTREAMABLEITEMS = 0x80000000
    }

    public enum SIGDN : uint
    {
        SIGDN_NORMALDISPLAY = 0,
        SIGDN_PARENTRELATIVEPARSING = 0x80018001,
        SIGDN_DESKTOPABSOLUTEPARSING = 0x80028000,
        SIGDN_PARENTRELATIVEEDITING = 0x80031001,
        SIGDN_DESKTOPABSOLUTEEDITING = 0x8004c000,
        SIGDN_FILESYSPATH = 0x80058000,
        SIGDN_URL = 0x80068000,
        SIGDN_PARENTRELATIVEFORADDRESSBAR = 0x8007c001,
        SIGDN_PARENTRELATIVE = 0x80080001,
        SIGDN_PARENTRELATIVEFORUI = 0x80094001
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct COMDLG_FILTERSPEC
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        public string pszName;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string pszSpec;
    }

    [ComImport]
    [Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IShellItem
    {
        void BindToHandler(IntPtr pbc, [In] ref Guid bhid, [In] ref Guid riid, out IntPtr ppv);
        void GetParent([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);
        void GetDisplayName([In] SIGDN sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);
        void GetAttributes([In] uint sfgaoMask, out uint psfgaoAttribs);
        void Compare([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi, [In] uint hint, out int piOrder);
    }

    [ComImport]
    [Guid("d57c7288-d4ad-4768-be02-9d969532d960")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IFileOpenDialog
    {
        [PreserveSig]
        int Show([In] IntPtr parent);
        void SetFileTypes([In] uint cFileTypes, [In] IntPtr rgFilterSpec);
        void SetFileTypeIndex([In] uint iFileType);
        void GetFileTypeIndex(out uint piFileType);
        void Advise([In, MarshalAs(UnmanagedType.Interface)] IntPtr pfde, out uint pdwCookie);
        void Unadvise([In] uint dwCookie);
        void SetOptions([In] FOS fos);
        void GetOptions(out FOS pfos);
        void SetDefaultFolder([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi);
        void SetFolder([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi);
        void GetFolder([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);
        void GetCurrentSelection([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);
        void SetFileName([In, MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);
        void SetTitle([In, MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
        void SetOkButtonLabel([In, MarshalAs(UnmanagedType.LPWStr)] string pszText);
        void SetFileNameLabel([In, MarshalAs(UnmanagedType.LPWStr)] string pszLabel);
        void GetResult([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);
        void AddPlace([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi, int fdap);
        void SetDefaultExtension([In, MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);
        void Close([MarshalAs(UnmanagedType.Error)] int hr);
        void SetClientGuid([In] ref Guid guid);
        void ClearClientData();
        void SetFilter([MarshalAs(UnmanagedType.Interface)] IntPtr pFilter);
        void GetResults([MarshalAs(UnmanagedType.Interface)] out IntPtr ppenum);
        void GetSelectedItems([MarshalAs(UnmanagedType.Interface)] out IntPtr ppsai);
    }

    [ComImport]
    [Guid("84bccd23-5fde-4cdb-aea4-af64b83d78ab")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IFileSaveDialog
    {
        [PreserveSig]
        int Show([In] IntPtr parent);
        void SetFileTypes([In] uint cFileTypes, [In] IntPtr rgFilterSpec);
        void SetFileTypeIndex([In] uint iFileType);
        void GetFileTypeIndex(out uint piFileType);
        void Advise([In, MarshalAs(UnmanagedType.Interface)] IntPtr pfde, out uint pdwCookie);
        void Unadvise([In] uint dwCookie);
        void SetOptions([In] FOS fos);
        void GetOptions(out FOS pfos);
        void SetDefaultFolder([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi);
        void SetFolder([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi);
        void GetFolder([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);
        void GetCurrentSelection([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);
        void SetFileName([In, MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);
        void SetTitle([In, MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
        void SetOkButtonLabel([In, MarshalAs(UnmanagedType.LPWStr)] string pszText);
        void SetFileNameLabel([In, MarshalAs(UnmanagedType.LPWStr)] string pszLabel);
        void GetResult([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);
        void AddPlace([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi, int fdap);
        void SetDefaultExtension([In, MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);
        void Close([MarshalAs(UnmanagedType.Error)] int hr);
        void SetClientGuid([In] ref Guid guid);
        void ClearClientData();
        void SetFilter([MarshalAs(UnmanagedType.Interface)] IntPtr pFilter);
        void SetSaveAsItem([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi);
        void SetProperties([In, MarshalAs(UnmanagedType.Interface)] IntPtr pStore);
        void SetCollectedProperties([In, MarshalAs(UnmanagedType.Interface)] IntPtr pList, [In] int fAppendDefault);
        void GetProperties([MarshalAs(UnmanagedType.Interface)] out IntPtr ppStore);
        void ApplyProperties([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi, [In, MarshalAs(UnmanagedType.Interface)] IntPtr pStore, [In] IntPtr hwnd, [In, MarshalAs(UnmanagedType.Interface)] IntPtr pSink);
    }

    [ComImport]
    [Guid("DC1C5A9C-E88A-4dde-A5A1-60F82A20AEF7")]
    [ClassInterface(ClassInterfaceType.None)]
    private class FileOpenDialogRCW { }

    [ComImport]
    [Guid("C0B4E2F3-BA21-4773-8DBA-335EC946EB8B")]
    [ClassInterface(ClassInterfaceType.None)]
    private class FileSaveDialogRCW { }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int SHCreateItemFromParsingName(
        [MarshalAs(UnmanagedType.LPWStr)] string pszPath,
        IntPtr pbc,
        ref Guid riid,
        [MarshalAs(UnmanagedType.Interface)] out IShellItem ppv);

    // Fallback SHBrowseForFolder
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct BROWSEINFO
    {
        public IntPtr hwndOwner;
        public IntPtr pidlRoot;
        public IntPtr pszDisplayName;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string lpszTitle;
        public uint ulFlags;
        public IntPtr lpfn;
        public IntPtr lParam;
        public int iImage;
    }

    private const uint BIF_RETURNONLYFSDIRS = 0x00000001;
    private const uint BIF_NEWDIALOGSTYLE = 0x00000040;
    private const uint BIF_USENEWUI = 0x00000040 | 0x00000010;

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SHBrowseForFolder(ref BROWSEINFO lpbi);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern bool SHGetPathFromIDList(IntPtr pidl, StringBuilder pszPath);

    [DllImport("ole32.dll")]
    private static extern void CoTaskMemFree(IntPtr pv);

    /// <summary>
    /// Ouvre la boîte de dialogue de sélection de dossier dans un thread STA dédié.
    /// Tente d'abord IFileOpenDialog, puis bascule sur SHBrowseForFolder en cas de problème.
    /// </summary>
    public static string? PickFolder(IntPtr parentHwnd, string? initialFolder = null, string? title = "Sélectionner un dossier")
    {
        string? result = null;
        Exception? caughtEx = null;

        AppLogService.Instance.LogInfo($"Demande de sélection de dossier. parentHwnd=0x{parentHwnd:X}, initialFolder='{initialFolder}'");

        var staThread = new Thread(() =>
        {
            try
            {
                // Tier 1 : IFileOpenDialog moderne
                result = ShowFileOpenDialog(parentHwnd, initialFolder, title);
                if (result != null)
                {
                    AppLogService.Instance.LogInfo($"Dossier sélectionné via IFileOpenDialog : '{result}'");
                    return;
                }
            }
            catch (Exception ex)
            {
                caughtEx = ex;
                AppLogService.Instance.LogWarning($"Échec IFileOpenDialog : {ex.Message}. Tentative via SHBrowseForFolder...");
            }

            // Si résultat null et pas d'annulation claire, ou exception : Tier 2 SHBrowseForFolder
            try
            {
                result = ShowBrowseForFolderFallback(parentHwnd, title);
                if (result != null)
                {
                    AppLogService.Instance.LogInfo($"Dossier sélectionné via SHBrowseForFolder : '{result}'");
                }
                else
                {
                    AppLogService.Instance.LogInfo("Sélection de dossier annulée par l'utilisateur.");
                }
            }
            catch (Exception fallbackEx)
            {
                AppLogService.Instance.LogError("Échec total des boîtes de dialogue de sélection de dossier.", fallbackEx);
            }
        });

        staThread.SetApartmentState(ApartmentState.STA);
        staThread.IsBackground = true;
        staThread.Start();
        staThread.Join();

        return result;
    }

    private static string? ShowFileOpenDialog(IntPtr parentHwnd, string? initialFolder, string? title)
    {
        var dialog = (IFileOpenDialog)new FileOpenDialogRCW();
        try
        {
            dialog.SetOptions(FOS.FOS_PICKFOLDERS | FOS.FOS_FORCEFILESYSTEM | FOS.FOS_PATHMUSTEXIST);
            if (!string.IsNullOrEmpty(title))
            {
                dialog.SetTitle(title);
            }

            if (!string.IsNullOrEmpty(initialFolder) && Directory.Exists(initialFolder))
            {
                try
                {
                    var shellItemGuid = new Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe");
                    if (SHCreateItemFromParsingName(initialFolder, IntPtr.Zero, ref shellItemGuid, out var initialItem) == 0 && initialItem != null)
                    {
                        dialog.SetFolder(initialItem);
                    }
                }
                catch
                {
                }
            }

            // Tente d'abord avec parentHwnd, ou IntPtr.Zero si parentHwnd invalide
            int hr = dialog.Show(parentHwnd);
            if (hr != 0 && parentHwnd != IntPtr.Zero)
            {
                // Si l'appel a échoué avec le handle parent (ex: restriction WinUI), réessaie sans parent
                hr = dialog.Show(IntPtr.Zero);
            }

            if (hr == 0) // S_OK
            {
                dialog.GetResult(out IShellItem item);
                if (item != null)
                {
                    item.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out string path);
                    return path;
                }
            }
            return null;
        }
        finally
        {
            Marshal.ReleaseComObject(dialog);
        }
    }

    private static string? ShowBrowseForFolderFallback(IntPtr parentHwnd, string? title)
    {
        var bi = new BROWSEINFO
        {
            hwndOwner = parentHwnd,
            lpszTitle = title ?? "Sélectionner un dossier",
            ulFlags = BIF_RETURNONLYFSDIRS | BIF_NEWDIALOGSTYLE
        };

        IntPtr pidl = SHBrowseForFolder(ref bi);
        if (pidl == IntPtr.Zero && parentHwnd != IntPtr.Zero)
        {
            // Réessaie sans parent
            bi.hwndOwner = IntPtr.Zero;
            pidl = SHBrowseForFolder(ref bi);
        }

        if (pidl != IntPtr.Zero)
        {
            try
            {
                var sb = new StringBuilder(1024);
                if (SHGetPathFromIDList(pidl, sb))
                {
                    return sb.ToString();
                }
            }
            finally
            {
                CoTaskMemFree(pidl);
            }
        }
        return null;
    }

    /// <summary>
    /// Ouvre la boîte de dialogue native pour sélectionner un fichier existant dans un thread STA dédié.
    /// </summary>
    public static string? PickFile(IntPtr parentHwnd, string? title = null, string? initialFolder = null)
    {
        string? result = null;
        AppLogService.Instance.LogInfo($"Demande de sélection de fichier. initialFolder='{initialFolder}'");

        var staThread = new Thread(() =>
        {
            var dialog = (IFileOpenDialog)new FileOpenDialogRCW();
            try
            {
                dialog.SetOptions(FOS.FOS_FORCEFILESYSTEM | FOS.FOS_FILEMUSTEXIST);
                if (!string.IsNullOrEmpty(title))
                {
                    dialog.SetTitle(title);
                }

                if (!string.IsNullOrEmpty(initialFolder) && Directory.Exists(initialFolder))
                {
                    try
                    {
                        var shellItemGuid = new Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe");
                        if (SHCreateItemFromParsingName(initialFolder, IntPtr.Zero, ref shellItemGuid, out var initialItem) == 0 && initialItem != null)
                        {
                            dialog.SetFolder(initialItem);
                        }
                    }
                    catch { }
                }

                int hr = dialog.Show(parentHwnd);
                if (hr != 0 && parentHwnd != IntPtr.Zero)
                {
                    hr = dialog.Show(IntPtr.Zero);
                }

                if (hr == 0)
                {
                    dialog.GetResult(out IShellItem item);
                    if (item != null)
                    {
                        item.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out string path);
                        result = path;
                        AppLogService.Instance.LogInfo($"Fichier sélectionné : '{result}'");
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogService.Instance.LogError("Erreur lors de la sélection de fichier", ex);
            }
            finally
            {
                Marshal.ReleaseComObject(dialog);
            }
        });

        staThread.SetApartmentState(ApartmentState.STA);
        staThread.IsBackground = true;
        staThread.Start();
        staThread.Join();

        return result;
    }

    /// <summary>
    /// Ouvre la boîte de dialogue native pour enregistrer un fichier dans un thread STA dédié.
    /// </summary>
    public static string? PickSaveFile(IntPtr parentHwnd, string extension, string filterName, string defaultFileName = "")
    {
        string? result = null;
        AppLogService.Instance.LogInfo($"Demande de sauvegarde de fichier. extension='{extension}', defaultFileName='{defaultFileName}'");

        var staThread = new Thread(() =>
        {
            var dialog = (IFileSaveDialog)new FileSaveDialogRCW();
            IntPtr filterSpecPtr = IntPtr.Zero;
            try
            {
                dialog.SetOptions(FOS.FOS_OVERWRITEPROMPT | FOS.FOS_FORCEFILESYSTEM | FOS.FOS_PATHMUSTEXIST);
                dialog.SetDefaultExtension(extension.TrimStart('.'));
                if (!string.IsNullOrEmpty(defaultFileName))
                {
                    dialog.SetFileName(defaultFileName);
                }

                var filter = new COMDLG_FILTERSPEC
                {
                    pszName = $"{filterName} (*{extension})",
                    pszSpec = $"*{extension}"
                };

                filterSpecPtr = Marshal.AllocHGlobal(Marshal.SizeOf(filter));
                Marshal.StructureToPtr(filter, filterSpecPtr, false);
                dialog.SetFileTypes(1, filterSpecPtr);

                int hr = dialog.Show(parentHwnd);
                if (hr != 0 && parentHwnd != IntPtr.Zero)
                {
                    hr = dialog.Show(IntPtr.Zero);
                }

                if (hr == 0)
                {
                    dialog.GetResult(out IShellItem item);
                    if (item != null)
                    {
                        item.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out string path);
                        result = path;
                        AppLogService.Instance.LogInfo($"Fichier de sauvegarde sélectionné : '{result}'");
                    }
                }
                else
                {
                    AppLogService.Instance.LogInfo("Sauvegarde de fichier annulée.");
                }
            }
            catch (Exception ex)
            {
                AppLogService.Instance.LogError("Erreur lors de la boîte de dialogue d'enregistrement", ex);
            }
            finally
            {
                if (filterSpecPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(filterSpecPtr);
                }
                Marshal.ReleaseComObject(dialog);
            }
        });

        staThread.SetApartmentState(ApartmentState.STA);
        staThread.IsBackground = true;
        staThread.Start();
        staThread.Join();

        return result;
    }
}
