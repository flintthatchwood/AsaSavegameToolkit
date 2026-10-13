using AsaSavegameToolkit.Plumbing.Records;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AsaSavegameToolkit.Plumbing.Readers
{
    public class ArkProfileReader:IDisposable
    {
        private readonly ILogger _logger;
        private readonly AsaReaderSettings _settings;

        public ArkProfileReader(ILogger? logger = default, AsaReaderSettings? settings = default)
        {
            _logger = logger ?? NullLogger.Instance;
            _settings = settings ?? AsaReaderSettings.None;
        }


        public void Dispose() { } // no unmanaged resources; IDisposable enables the `using` pattern

        public List<GameObjectRecord> Read(string saveDirectory, CancellationToken cancellationToken = default)
        {
            var tribeFiles = Directory.EnumerateFiles(saveDirectory, "*.arkprofile");

            ConcurrentBag<GameObjectRecord> profileBag = new();
            Parallel.ForEach(tribeFiles, new ParallelOptions { MaxDegreeOfParallelism = _settings.MaxCores }, filePath =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var profileGameObject = ArkProfileRecord.ReadFromFile(filePath, Guid.NewGuid());
                    if (profileGameObject != null)
                    {
                        profileBag.Add(profileGameObject);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to read profile file {FilePath}", filePath);
                }

            });

            return profileBag.ToList();
        }

    }
}
