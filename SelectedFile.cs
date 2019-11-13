using EPDM.Interop.epdm;

namespace PRS
{
    internal class SelectedFile
    {
        public IEdmVault5 vault { get; }
        public string Name { get; }
        public string State { get; }
        public string Path { get; set; } = "";
        public string User { get; } = "";
        public int FileID { get; }
        public int FolderID { get; }
        public IEdmFile5 File { get; } = null;
        public IEdmFolder5 Folder { get; } = null;
        public SelectedFile(IEdmSearchResult5 Result)
        {
            if (Result != null)
            {                
                FileID = Result.ID;
                FolderID = Result.ParentFolderID;
                Name = Result.Name;
                if (Result.StateName == "") State = "Закрытое состояние";
                else State = Result.StateName;
                Path = Result.Path;
                vault = Result.Vault;
                User = Result.LockedByUserName;  
            }
        }     
        public SelectedFile(IEdmReference7 Reference)
        {
            if (Reference != null)
            {                
                File = Reference.File;
                Folder = Reference.Folder;
                FileID = Reference.FileID;
                FolderID = Reference.FolderID;
                Name = Reference.Name;
                State = Reference.File.CurrentState.Name;
                Path = Reference.FoundPath;
                vault = Reference.File.Vault; 
                if (Reference.IsLocked) User = Reference.LockedByUser.Name;                
            }
        }
        public SelectedFile(IEdmFile5 file, IEdmFolder5 folder)
        {
            vault = File.Vault;
            File = file;
            Folder = folder;
            FileID = file.ID;
            FolderID = folder.ID;
            Name = file.Name;
            State = file.CurrentState.Name;
            Path = System.IO.Path.Combine(folder.LocalPath, file.Name);            
            File = file;
            Folder = folder;
            if (file.IsLocked) User = file.LockedByUser.Name;
        }
    }
}
