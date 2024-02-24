using DamageBoy.Core;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;

namespace DamageBoy.UI;

class FileBrowserUI : BaseUI
{
    readonly string windowTitle;
    readonly List<string> allowedExtensions;
    readonly bool onlyAllowDirectories;
    readonly Action<string> openFileCallback;

    readonly EnumerationOptions enumerationOptions;
    readonly List<DirectoryEntry> currentDirectoryEntries;

    string selectedEntry;

    string currentDirectory;
    string CurrentDirectory
    {
        get => currentDirectory;
        set
        {
            if (currentDirectory == value) return;
            if (!Directory.Exists(value)) return;
            currentDirectory = value;
            PopulateFileSystemEntries();
        }
    }

    struct DirectoryEntry
    {
        public string FullPath { get; set; }
        public string EntryName { get; set; }
        public bool IsDirectory { get; set; }

        public DirectoryEntry(string fullPath, string entryName, bool isDirectory)
        {
            FullPath = fullPath;
            EntryName = entryName;
            IsDirectory = isDirectory;
        }
    }

    const int ELEMENTS_WIDTH = 800;
    const int HORIZONTAL_SEPARATION = 8;
    const int BUTTON_WIDTH = 100;

    public FileBrowserUI(string windowTitle, string startingPath, string searchFilter = null, bool onlyAllowDirectories = false, Action<string> openFileCallback = null)
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

        currentDirectoryEntries = new List<DirectoryEntry>();

        if (!string.IsNullOrWhiteSpace(searchFilter))
        {
            allowedExtensions = new List<string>();
            allowedExtensions.AddRange(searchFilter.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries));
        }

        this.onlyAllowDirectories = onlyAllowDirectories;
        this.openFileCallback = openFileCallback;

        if (!string.IsNullOrWhiteSpace(startingPath) && Directory.Exists(startingPath))
        {
            currentDirectory = startingPath;
        }
        else
        {
            currentDirectory = Environment.CurrentDirectory;
            if (string.IsNullOrWhiteSpace(currentDirectory)) currentDirectory = AppContext.BaseDirectory;
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
            if (Directory.Exists(CurrentDirectory))
            {
                DirectoryInfo info = Directory.GetParent(CurrentDirectory);
                if (info != null)
                {
                    CurrentDirectory = info.FullName;
                }
            }
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            ImGui.SameLine();
            ImGui.PushItemWidth(ELEMENTS_WIDTH - ((BUTTON_WIDTH + HORIZONTAL_SEPARATION) * 2));

            if (ImGui.Combo("##Drive", ref currentDriveIndex, drivesNames, drivesNames.Length))
            {
                CurrentDriveChanged();
            }

            ImGui.PopItemWidth();
        }

        ImGui.PushItemWidth(ELEMENTS_WIDTH);

        string cf = CurrentDirectory;
        if (ImGui.InputText("##Current Directory", ref cf, 512))
        {
            CurrentDirectory = cf;
            selectedEntry = null;
            SetDriveIndexFromDirectory();
        }

        ImGui.PopItemWidth();

        if (ImGui.BeginChild(1, new Vector2(ELEMENTS_WIDTH, 400)))
        {
            ImGuiListClipperPtr listClipper;

            unsafe
            {
                listClipper = new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper_ImGuiListClipper());
            }

            listClipper.Begin(currentDirectoryEntries.Count);

            while (listClipper.Step())
            {
                bool changedDirectory = false;

                for (int e = listClipper.DisplayStart; e < listClipper.DisplayEnd; e++)
                {
                    DirectoryEntry entry = currentDirectoryEntries[e];

                    if (entry.IsDirectory)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 1f, 0f, 1f));
                        changedDirectory = ImGui.Selectable(entry.EntryName, false, ImGuiSelectableFlags.DontClosePopups);
                        ImGui.PopStyleColor();

                        if (changedDirectory)
                        {
                            CurrentDirectory = entry.FullPath;
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

                if (changedDirectory) break;
            }

            ImGui.EndChild();
        }

        if (ImGui.Button("Cancel", new Vector2(BUTTON_WIDTH, 0)))
        {
            Close();
        }

        if (onlyAllowDirectories)
        {
            ImGui.SameLine();

            if (ImGui.Button("Open", new Vector2(BUTTON_WIDTH, 0)))
            {
                openFileCallback?.Invoke(CurrentDirectory);
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
        if (string.IsNullOrWhiteSpace(CurrentDirectory))
        {
            return;
        }

        try
        {
            string[] directories = Directory.GetDirectories(CurrentDirectory, string.Empty, enumerationOptions);
            Array.Sort(directories); // On linux, directories and files might be obtained in random order

            currentDirectoryEntries.Clear();

            for (int d = 0; d < directories.Length; d++)
            {
                currentDirectoryEntries.Add(new DirectoryEntry(directories[d], Path.GetFileName(directories[d]) + "/", true));
            }

            if (onlyAllowDirectories) return;

            string[] files = Directory.GetFiles(CurrentDirectory, string.Empty);
            Array.Sort(files);

            for (int f = 0; f < files.Length; f++)
            {
                if (allowedExtensions != null)
                {
                    var extension = Path.GetExtension(files[f]);

                    if (allowedExtensions.Contains(extension))
                    {
                        currentDirectoryEntries.Add(new DirectoryEntry(files[f], Path.GetFileName(files[f]), false));
                    }
                }
                else
                {
                    currentDirectoryEntries.Add(new DirectoryEntry(files[f], Path.GetFileName(files[f]), false));
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

    string[] drivesNames = null;
    int currentDriveIndex = 0;

    void UpdateDrives()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;

        DriveInfo[] drives = DriveInfo.GetDrives();

        List<string> driveNamesList = new List<string>();

        for (int d = 0; d < drives.Length; d++)
        {
            if (!drives[d].IsReady) continue;
            driveNamesList.Add(drives[d].Name);
        }

        drivesNames = driveNamesList.ToArray();

        if (currentDriveIndex < 0 || currentDriveIndex >= drivesNames.Length)
        {
            currentDriveIndex = 0;
            CurrentDriveChanged();
        }
    }

    void SetDriveIndexFromDirectory()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;

        DirectoryInfo info;

        try
        {
            info = new DirectoryInfo(CurrentDirectory);
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

        for (int d = 0; d < drivesNames.Length; d++)
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
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;

        selectedEntry = null;
        CurrentDirectory = drivesNames[currentDriveIndex];
    }

    #endregion
}