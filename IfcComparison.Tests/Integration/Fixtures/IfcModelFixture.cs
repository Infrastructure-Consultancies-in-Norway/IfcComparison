using System;
using System.IO;
using Xbim.Ifc;

namespace IfcComparison.Tests.Integration.Fixtures
{
    /// <summary>
    /// Base fixture class for loading IFC file pairs for integration testing.
    /// Implements IDisposable to ensure proper cleanup of IfcStore resources.
    /// </summary>
    public abstract class IfcModelFixture : IDisposable
    {
        protected IfcModelFixture(string oldFilePath, string newFilePath, string qaFilePath = null)
        {
            OldFilePath = oldFilePath;
            NewFilePath = newFilePath;
            QAFilePath = qaFilePath ?? newFilePath.Replace(".ifc", "_QA.ifc");

            LoadModels();
        }

        public string OldFilePath { get; }
        public string NewFilePath { get; }
        public string QAFilePath { get; }

        public IfcStore OldModel { get; private set; }
        public IfcStore NewModel { get; private set; }
        public IfcStore QAModel { get; private set; }

        public bool ModelsLoaded => OldModel != null && NewModel != null;

        private void LoadModels()
        {
            try
            {
                // Load old model
                if (File.Exists(OldFilePath))
                {
                    OldModel = IfcStore.Open(OldFilePath);
                }
                else
                {
                    throw new FileNotFoundException($"Old IFC file not found: {OldFilePath}");
                }

                // Load new model
                if (File.Exists(NewFilePath))
                {
                    NewModel = IfcStore.Open(NewFilePath);
                }
                else
                {
                    throw new FileNotFoundException($"New IFC file not found: {NewFilePath}");
                }

                // Copy new model to QA path for testing
                if (File.Exists(NewFilePath))
                {
                    File.Copy(NewFilePath, QAFilePath, overwrite: true);
                    QAModel = IfcStore.Open(QAFilePath);
                }
            }
            catch (Exception ex)
            {
                // Clean up any partially loaded models
                Dispose();
                throw new InvalidOperationException($"Failed to load IFC models: {ex.Message}", ex);
            }
        }

        public void Dispose()
        {
            OldModel?.Dispose();
            NewModel?.Dispose();
            QAModel?.Dispose();

            // Optionally clean up QA file
            try
            {
                if (File.Exists(QAFilePath))
                {
                    File.Delete(QAFilePath);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
