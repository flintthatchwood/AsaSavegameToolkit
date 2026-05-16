using AsaSavegameToolkit.Plumbing.Properties;
using AsaSavegameToolkit.Plumbing.Readers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsaSavegameToolkit.Plumbing.Records
{
    public class ArkTribeRecord
    {

        private static GameObjectRecord ReadPre7(AsaArchive archive, Guid uuid)
        {
            var tribeCount = archive.ReadInt32();
            

            //start game object
            var uuid2 = archive.ReadGuid();
            var className = archive.ReadString();
            _ = archive.ReadInt32();

            var nameCount = archive.ReadInt32();
            List<string> names = new List<string>();
            while(nameCount-- > 0)
            {
                var name = archive.ReadString();
                names.Add(name);
            }
            
            _  = archive.ReadInt32();
            _  = archive.ReadInt32();
            var hasLocation = archive.ReadInt32() != 0;
            if (hasLocation)
            {


            }
            var propertiesOffset = archive.ReadInt32();

            archive.Position= propertiesOffset;

            var properties = Property.ReadList(archive);

            // end game object
            ObjectTypeFlags objectType = ObjectTypeFlags.Actor;

            return new GameObjectRecord(uuid, new Primitives.FName(0, 0, className), names, properties, 0, objectType, default);

        }

        public static GameObjectRecord Read(AsaArchive archive, Guid uuid)
        {
            string mapName = string.Empty;

            // Header sequence
            var fileVersion = archive.ReadInt32(); // tribeVersion
            archive.SaveVersion = (short)fileVersion;
            archive.IsArkFile = true;
            if (fileVersion < 7)
            {
                return ReadPre7(archive, uuid);
            }

            _ = archive.ReadBytes(12); // ID, Save Count, Table Offset
            _ = archive.ReadBytes(16); // GUID

            var fileType = archive.ReadString();
            _ = archive.ReadInt32();

            var names = archive.ReadStringArray(); // Map Name table strings
            if (names != null && names.Length > 3)
            {
                mapName = names[3]; // Map name
            }

            _ = archive.ReadInt32(); // shouldBeZero
            _ = archive.ReadBytes(16); // Padding/Struct ID
            _ = archive.ReadBytes(1);  // Separator

            //Properties
            var properties = Property.ReadList(archive);
            ObjectTypeFlags objectType = ObjectTypeFlags.Actor;

            return new GameObjectRecord(uuid, new Primitives.FName(0, 0, fileType), names, properties, 0, objectType, default);
        }

        public static GameObjectRecord ReadFromFile(string filePath, Guid uuid)
        {

            var timestamp = File.GetLastWriteTimeUtc(filePath);
            using var archive = new AsaArchive(NullLogger.Instance, File.ReadAllBytes(filePath), filePath);

            return Read(archive, uuid);
        }

    }
}
