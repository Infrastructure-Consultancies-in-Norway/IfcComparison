
using IfcComparison.Enumerations;
using IfcComparison.Logging;
using IfcComparison.ViewModels;
using System.Threading.Tasks;
using WpfUtilities.Utils;
using Xbim.Ifc;
using Xunit;

namespace IfcComparison.Tests
{
    public class IfcComparerObjects
    {
        static IfcComparerObjects()
        {
            // Initialize logging before any tests run
            LoggerInitializer.EnsureInitialized();
        }

        private static IfcStore OldIfcModel { get; set; }
        private static IfcStore NewIfcModel { get; set; }

        private static List<string> _pSetToCheck = new List<string> { "SOS-KON_Armering", "SOS-KON_Felles" };

        private static IfcEntity _entity = new IfcEntity() { ComparisonMethod = ComparisonEnumeration.Contains.ToString(), ComparisonOperator = "ARM.07", Entity = "IIfcReinforcingBar", IfcPropertySets = _pSetToCheck };
        private static IfcEntity _entity_Id = new IfcEntity() { ComparisonMethod = ComparisonEnumeration.Identifier.ToString(), ComparisonOperator = "", Entity = "IIfcReinforcingBar", IfcPropertySets = _pSetToCheck };

        private static List<IfcEntity> _ifcEntities = new List<IfcEntity>()
        {
            _entity,
        };

        private static List<IfcEntity> _ifcEntities_Id = new List<IfcEntity>()
        {
            _entity_Id,
        };

        // Initialize the IfcModel helper method before running tests
        public async Task<IfcStore> Initialize_IfcModel(string ifcFilePath)
        {
            var ifc = IfcStore.Open(ifcFilePath);
            if (ifc == null)
            {
                throw new ArgumentNullException(nameof(ifc), "IfcStore could not be opened. Please check the file path.");
            }
            return ifc;
        }

        // Initialize the OldIfcModel
        [Fact]
        public async Task Initialize_OldIfcModel()
        {
            // Call the helper method to initialize the model
            var ifcFilePath = @"../../../IfcFiles/Nordstrand/SOS_05NOR_F_KON_OVERGANGSBRU_03C.ifc";

            OldIfcModel = await Initialize_IfcModel(ifcFilePath);

            Assert.NotNull(OldIfcModel);
        }

        // Initialize the OldIfcModel
        [Fact]
        public async Task Initialize_NewIfcModel()
        {
            // Call the helper method to initialize the model
            var ifcFilePath = @"../../../IfcFiles/Nordstrand/SOS_05NOR_F_KON_OVERGANGSBRU_04C.ifc";
            var ifcFilePathQA = @"../../../IfcFiles/Nordstrand/SOS_05NOR_F_KON_OVERGANGSBRU_04C_QA.ifc";
            File.Copy(ifcFilePath, ifcFilePathQA, true); // Copy the file to avoid modifying the original

            NewIfcModel = await Initialize_IfcModel(ifcFilePathQA);

            Assert.NotNull(NewIfcModel);

        }


        [Fact]
        // Check if the IfcComparerObjects class can be instantiated with a valid IfcStore and IfcEntity
        public void Initialize_OldIfcComparerObjects()
        {
            if (OldIfcModel == null)
            {
                // Initialize the OldIfcModel if it hasn't been done yet
                Task.Run(() => Initialize_OldIfcModel());
            }

            var ifcComparerObjects = Models.IfcComparerObjects.CreateAsync(OldIfcModel, _entity);
            Assert.NotNull(ifcComparerObjects);
        }

        [Fact]
        public async Task Initialize_IfcComparer()
        {
            if (OldIfcModel == null || NewIfcModel == null)
            {
                // Initialize the models if they haven't been done yet
                var task = Task.WhenAll(
                    Initialize_OldIfcModel(),
                    Initialize_NewIfcModel()
                );
                await task; // Wait for both initializations to complete
            }

            var filePathIfcQA = @"../../../IfcFiles/Nordstrand/SOS_05NOR_F_KON_OVERGANGSBRU_04C_QA.ifc";

            var ifcComparerTask = Models.IfcComparer.CreateAsync(OldIfcModel, NewIfcModel, filePathIfcQA, "Test Transaction", _ifcEntities);
            var ifcComparer = await ifcComparerTask;

            await ifcComparer.CompareAllRevisions(); // Ensure the task completes before proceeding



        }

        [Fact]
        public async Task Initialize_IfcComparer_Identifier()
        {
            if (OldIfcModel == null || NewIfcModel == null)
            {
                // Initialize the models if they haven't been done yet
                var task = Task.WhenAll(
                    Initialize_OldIfcModel(),
                    Initialize_NewIfcModel()
                );
                await task; // Wait for both initializations to complete
            }

            var filePathIfcQA = @"../../../IfcFiles/Nordstrand/SOS_05NOR_F_KON_OVERGANGSBRU_04C_QA.ifc";

            var ifcComparerTask = Models.IfcComparer.CreateAsync(OldIfcModel, NewIfcModel, filePathIfcQA, "Test Transaction", _ifcEntities_Id);
            var ifcComparer = await ifcComparerTask;

            await ifcComparer.CompareAllRevisions(); // Ensure the task completes before proceeding



        }

        [Fact]
        public async Task Initialize_IfcComparer_And_Compare_JsonInput()
        {
            if (OldIfcModel == null || NewIfcModel == null)
            {
                // Initialize the models if they haven't been done yet
                var task = Task.WhenAll(
                    Initialize_OldIfcModel(),
                    Initialize_NewIfcModel()
                );
                await task; // Wait for both initializations to complete
            }

            var filePathIfcQA = @"../../../IfcFiles/Nordstrand/SOS_05NOR_F_KON_OVERGANGSBRU_04C_QA.ifc";


            var jsonInputPath = @"C:\\Users\\fkjn\\Downloads\\TestSettings.json";
            var userSettings = (UserSettings)ReadAndWriteJson.ReadAndDeserialize<UserSettings>(jsonInputPath);
            var enteties = userSettings.DataGridContentIFCEntities.ToList();


            var ifcComparerTask = Models.IfcComparer.CreateAsync(OldIfcModel, NewIfcModel, filePathIfcQA, "Test Transaction", enteties);
            var ifcComparer = await ifcComparerTask;

            await ifcComparer.CompareAllRevisions(); // Ensure the task completes before proceeding



        }










    }
}
