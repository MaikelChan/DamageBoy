using DamageBoy.Core;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace DamageBoy.UI;

class FileBrowserUI : BaseUI
{
    readonly string windowTitle;
    readonly List<string> allowedExtensions;
    readonly bool onlyAllowFolders;
    readonly Action<string> openFileCallback;

    readonly EnumerationOptions enumerationOptions;
    readonly List<FolderEntry> currentFolderEntries;

    string selectedEntry;

    string currentFolder;
    string CurrentFolder
    {
        get => currentFolder;
        set
        {
            if (currentFolder == value) return;
            if (!Directory.Exists(value)) return;
            currentFolder = value;
            PopulateFileSystemEntries();
        }
    }

    struct FolderEntry
    {
        public string FullPath { get; set; }
        public string EntryName { get; set; }
        public bool IsDirectory { get; set; }

        public FolderEntry(string fullPath, string entryName, bool isDirectory)
        {
            FullPath = fullPath;
            EntryName = entryName;
            IsDirectory = isDirectory;
        }
    }

    const int ELEMENTS_WIDTH = 800;
    const int HORIZONTAL_SEPARATION = 8;
    const int BUTTON_WIDTH = 100;

    public FileBrowserUI(string windowTitle, string startingPath, string searchFilter = null, bool onlyAllowFolders = false, Action<string> openFileCallback = null)
    {
        if (!string.IsNullOrWhiteSpace(windowTitle))
        {
            this.windowTitle = windowTitle;
        }
        else
        {
            this.windowTitle = "Open File";
        }

        enumerationOptions = new EnumerationOptions()
        {
            MatchType = MatchType.Win32,
            ReturnSpecialDirectories = false,
            AttributesToSkip = FileAttributes.Hidden | FileAttributes.System
        };

        currentFolderEntries = new List<FolderEntry>();

        if (!string.IsNullOrWhiteSpace(searchFilter))
        {
            allowedExtensions = new List<string>();
            allowedExtensions.AddRange(searchFilter.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries));
        }

        this.onlyAllowFolders = onlyAllowFolders;
        this.openFileCallback = openFileCallback;

        if (!string.IsNullOrWhiteSpace(startingPath) && Directory.Exists(startingPath))
        {
            currentFolder = startingPath;
        }
        else
        {
            currentFolder = Environment.CurrentDirectory;
            if (string.IsNullOrWhiteSpace(currentFolder)) currentFolder = AppContext.BaseDirectory;
        }
    }

    protected override void VisibilityChanged(bool isVisible)
    {
        base.VisibilityChanged(isVisible);

        if (isVisible)
        {
            ImGui.OpenPopup(windowTitle);

            Refresh();
        }
    }

    protected override void InternalRender()
    {
        if (!ImGui.BeginPopupModal(windowTitle, ref isVisible, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse))
        {
            ImGui.EndPopup();
            return;
        }

        if (ImGui.Button("Refresh", new Vector2(BUTTON_WIDTH, 0)))
        {
            Refresh();
        }

        ImGui.SameLine();

        if (ImGui.Button("Go to Parent", new Vector2(BUTTON_WIDTH, 0)))
        {
            if (Directory.Exists(CurrentFolder))
            {
                DirectoryInfo info = Directory.GetParent(CurrentFolder);
                if (info != null)
                {
                    CurrentFolder = info.FullName;
                }
            }
        }

        ImGui.SameLine();
        ImGui.PushItemWidth(ELEMENTS_WIDTH - ((BUTTON_WIDTH + HORIZONTAL_SEPARATION) * 2));

        if (ImGui.Combo("##Drive", ref currentDriveIndex, drivesNames[0..drivesCount], drivesCount))
        {
            CurrentDriveChanged();
        }

        ImGui.PopItemWidth();

        ImGui.PushItemWidth(ELEMENTS_WIDTH);

        string cf = CurrentFolder;
        if (ImGui.InputText("##Current Folder", ref cf, 512))
        {
            CurrentFolder = cf;

            if (string.IsNullOrWhiteSpace(CurrentFolder))
            {
                CurrentDriveChanged();
            }

            selectedEntry = null;
            SetDriveIndexFromDirectory();
        }

        ImGui.PopItemWidth();

        if (ImGui.BeginChildFrame(1, new Vector2(ELEMENTS_WIDTH, 400)))
        {
            ImGuiListClipperPtr listClipper;

            unsafe
            {
                listClipper = new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper_ImGuiListClipper());
            }

            listClipper.Begin(currentFolderEntries.Count);

            while (listClipper.Step())
            {
                for (int e = listClipper.DisplayStart; e < listClipper.DisplayEnd; e++)
                {
                    FolderEntry entry = currentFolderEntries[e];

                    if (entry.IsDirectory)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 1f, 0f, 1f));
                        bool clicked = ImGui.Selectable(entry.EntryName, false, ImGuiSelectableFlags.DontClosePopups);
                        ImGui.PopStyleColor();

                        if (clicked)
                        {
                            CurrentFolder = entry.FullPath;
                            selectedEntry = null;
                            break;
                        }
                    }
                    else
                    {
                        bool isSelected = selectedEntry == entry.FullPath;
                        bool clicked = ImGui.Selectable(entry.EntryName, isSelected, ImGuiSelectableFlags.DontClosePopups);

                        if (clicked)
                        {
                            selectedEntry = entry.FullPath;
                        }
                    }
                }
            }

            ImGui.EndChildFrame();
        }

        if (ImGui.Button("Cancel", new Vector2(BUTTON_WIDTH, 0)))
        {
            Close();
        }

        if (onlyAllowFolders)
        {
            ImGui.SameLine();

            if (ImGui.Button("Open", new Vector2(BUTTON_WIDTH, 0)))
            {
                openFileCallback?.Invoke(CurrentFolder);
                Close();
            }
        }
        else if (selectedEntry != null)
        {
            ImGui.SameLine();

            if (ImGui.Button("Open", new Vector2(BUTTON_WIDTH, 0)))
            {
                openFileCallback?.Invoke(selectedEntry);
                Close();
            }
        }

        ImGui.EndPopup();
    }

    void Close()
    {
        ImGui.CloseCurrentPopup();
        isVisible = false;
    }

    void PopulateFileSystemEntries()
    {
        if (string.IsNullOrWhiteSpace(CurrentFolder))
        {
            return;
        }

        try
        {
            string[] directories = Directory.GetDirectories(CurrentFolder, string.Empty, enumerationOptions);

            currentFolderEntries.Clear();

            for (int d = 0; d < directories.Length; d++)
            {
                currentFolderEntries.Add(new FolderEntry(directories[d], Path.GetFileName(directories[d]) + "/", true));
            }

            if (onlyAllowFolders) return;

            string[] files = Directory.GetFiles(CurrentFolder, string.Empty);

            for (int f = 0; f < files.Length; f++)
            {
                if (allowedExtensions != null)
                {
                    var extension = Path.GetExtension(files[f]);

                    if (allowedExtensions.Contains(extension))
                    {
                        currentFolderEntries.Add(new FolderEntry(files[f], Path.GetFileName(files[f]), false));
                    }
                }
                else
                {
                    currentFolderEntries.Add(new FolderEntry(files[f], Path.GetFileName(files[f]), false));
                }
            }
        }
        catch (Exception ex)
        {
            Utils.Log(LogType.Error, ex.Message);
            return;
        }
    }

    void Refresh()
    {
        UpdateDrives();
        SetDriveIndexFromDirectory();
        PopulateFileSystemEntries();
    }

    #region Drive handling

    readonly string[] drivesNames = new string[WINDOWS_MAX_DRIVES];
    int drivesCount = 0;
    int currentDriveIndex = 0;

    const int WINDOWS_MAX_DRIVES = 26;

    void UpdateDrives()
    {
        drivesCount = 0;

        DriveInfo[] drives = DriveInfo.GetDrives();

        for (int d = 0; d < drives.Length; d++)
        {
            if (!drives[d].IsReady) continue;
            drivesNames[drivesCount] = drives[d].Name;
            drivesCount++;
        }

        if (currentDriveIndex < 0 || currentDriveIndex >= drivesCount)
        {
            currentDriveIndex = 0;
            CurrentDriveChanged();
        }
    }

    void SetDriveIndexFromDirectory()
    {
        DirectoryInfo info;

        try
        {
            info = new DirectoryInfo(CurrentFolder);
        }
        catch (Exception ex)
        {
            Utils.Log(LogType.Error, ex.Message);
            currentDriveIndex = 0;
            return;
        }

        if (!info.Exists)
        {
            currentDriveIndex = 0;
            return;
        }

        for (int d = 0; d < drivesCount; d++)
        {
            if (drivesNames[d] == info.Root.Name)
            {
                currentDriveIndex = d;
                return;
            }
        }

        currentDriveIndex = 0;
    }

    void CurrentDriveChanged()
    {
        selectedEntry = null;
        CurrentFolder = drivesNames[currentDriveIndex];
    }

    #endregion
}