using Archive.Data;

namespace Archive
{
    public interface IArchivable
    {
        public void Save(ArchiveData archiveData);
        public void Load(ArchiveData archiveData);
    }
}