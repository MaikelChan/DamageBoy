using DamageBoy.Core;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace DamageBoy.UI
{
    class FileBrowserUI : BaseUI
    {
        readonly string windowTitle;
        readonly List<string> allowedExtensions;
        readonly bool onlyAllowFolders;
        readonly Action<string> openFileCallback;

        readonly EnumerationOptions enumerationOptions;
        readonly List<string> currentFolderEntries;

        string selectedEntry;
        string currentFolder;

        const int ELEMENTS_WIDTH = 600;

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

            if (!string.IsNullOrWhiteSpace(startingPath) && Directory.Exists(startingPath))
            {
                currentFolder = startingPath;
            }
            else
            {
                currentFolder = Environment.CurrentDirectory;
                if (string.IsNullOrWhiteSpace(currentFolder)) currentFolder = AppContext.BaseDirectory;
            }

            if (!string.IsNullOrWhiteSpace(searchFilter))
            {
                allowedExtensions = new List<string>();
                allowedExtensions.AddRange(searchFilter.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries));
            }

            this.onlyAllowFolders = onlyAllowFolders;
            this.openFileCallback = openFileCallback;

            UpdateDrives();
            SetDriveIndexFromDirectory(currentFolder);

            currentFolderEntries = new List<string>();

            enumerationOptions = new EnumerationOptions()
            {
                MatchType = MatchType.Win32,
                ReturnSpecialDirectories = true,
                AttributesToSkip = FileAttributes.Hidden | FileAttributes.System
            };
        }

        protected override void VisibilityChanged(bool isVisible)
        {
            base.VisibilityChanged(isVisible);

            if (isVisible)
            {
                ImGui.OpenPopup(windowTitle);
            }
        }

        protected override void InternalRender()
        {
            if (!ImGui.BeginPopupModal(windowTitle, ref isVisible, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse))
            {
                ImGui.EndPopup();
                return;
            }

            UpdateDrives();

            ImGui.PushItemWidth(ELEMENTS_WIDTH);

            if (ImGui.Combo("##Drive", ref currentDriveIndex, drivesNames.ToArray(), drivesNames.Count))
            {
                CurrentDriveChanged();
            }

            if (ImGui.InputText("##Current Folder", ref currentFolder, 512))
            {
                if (string.IsNullOrWhiteSpace(currentFolder))
                {
                    CurrentDriveChanged();
                }

                selectedEntry = null;
                SetDriveIndexFromDirectory(currentFolder);
            }

            ImGui.PopItemWidth();

            if (ImGui.BeginChildFrame(1, new Vector2(ELEMENTS_WIDTH, 400)))
            {
                var directory = new DirectoryInfo(currentFolder);

                if (directory.Exists)
                {
                    PopulateFileSystemEntries(directory.FullName);

                    foreach (var entry in currentFolderEntries)
                    {
                        if (Directory.Exists(entry))
                        {
                            var name = Path.GetFileName(entry);
                            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 1f, 0f, 1f));
                            if (ImGui.Selectable(name + "/", false, ImGuiSelectableFlags.DontClosePopups))
                            {
                                currentFolder = entry;
                                selectedEntry = null;
                            }
                            ImGui.PopStyleColor();
                        }
                        else
                        {
                            var name = Path.GetFileName(entry);
                            bool isSelected = selectedEntry == entry;
                            if (ImGui.Selectable(name, isSelected, ImGuiSelectableFlags.DontClosePopups))
                                selectedEntry = entry;
                        }
                    }
                }

                ImGui.EndChildFrame();
            }

            if (ImGui.Button("Cancel", new Vector2(120, 0)))
            {
                Close();
            }

            if (onlyAllowFolders)
            {
                ImGui.SameLine();

                if (ImGui.Button("Open", new Vector2(120, 0)))
                {
                    openFileCallback?.Invoke(currentFolder);
                    Close();
                }
            }
            else if (selectedEntry != null)
            {
                ImGui.SameLine();

                if (ImGui.Button("Open", new Vector2(120, 0)))
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

        void PopulateFileSystemEntries(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                return;
            }

            string[] directories = Directory.GetDirectories(directory, string.Empty, enumerationOptions);

            currentFolderEntries.Clear();
            currentFolderEntries.AddRange(directories);

            if (!onlyAllowFolders)
            {
                string[] files;

                try
                {
                    files = Directory.GetFiles(directory, string.Empty);
                }
                catch (UnauthorizedAccessException ex)
                {
                    Utils.Log(LogType.Warning, ex.Message);
                    return;
                }

                for (int f = 0; f < files.Length; f++)
                {
                    if (allowedExtensions != null)
                    {
                        var extension = Path.GetExtension(files[f]);

                        if (allowedExtensions.Contains(extension))
                        {
                            currentFolderEntries.Add(files[f]);
                        }
                    }
                    else
                    {
                        currentFolderEntries.Add(files[f]);
                    }
                }
            }
        }

        #region Drive handling

        readonly List<string> drivesNames = new List<string>();
        int currentDriveIndex = 0;

        void UpdateDrives()
        {
            drivesNames.Clear();

            DriveInfo[] drives = DriveInfo.GetDrives();

            for (int d = 0; d < drives.Length; d++)
            {
                if (!drives[d].IsReady) continue;
                drivesNames.Add(drives[d].Name);
            }

            if (currentDriveIndex < 0 || currentDriveIndex >= drivesNames.Count)
            {
                currentDriveIndex = 0;
                CurrentDriveChanged();
            }
        }

        void SetDriveIndexFromDirectory(string directory)
        {
            DirectoryInfo info;

            try
            {
                info = new DirectoryInfo(directory);
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

            for (int d = 0; d < drivesNames.Count; d++)
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
            currentFolder = drivesNames[currentDriveIndex];
        }

        #endregion
    }
}