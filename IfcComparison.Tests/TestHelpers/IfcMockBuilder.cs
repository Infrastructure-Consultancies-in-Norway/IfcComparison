using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using Xbim.Common;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.UtilityResource;

namespace IfcComparison.Tests.TestHelpers
{
    /// <summary>
    /// Builder class for creating mocked IFC objects for testing
    /// </summary>
    public class IfcMockBuilder
    {
        /// <summary>
        /// Creates a mock IfcStore with configurable instances
        /// </summary>
        public static Mock<IfcStore> CreateMockIfcStore()
        {
            var mockStore = new Mock<IfcStore>();
            var mockInstances = new Mock<IEntityCollection>();
            
            mockStore.Setup(s => s.Instances).Returns(mockInstances.Object);
            
            return mockStore;
        }

        /// <summary>
        /// Creates a mock IIfcObject with specified GlobalId and entity label
        /// </summary>
        public static Mock<IIfcObject> CreateMockIfcObject(string globalId = null, int entityLabel = 1)
        {
            var mockObject = new Mock<IIfcObject>();
            var guidValue = string.IsNullOrEmpty(globalId) ? Guid.NewGuid().ToString() : globalId;
            
            mockObject.Setup(o => o.GlobalId).Returns(new IfcGloballyUniqueId(guidValue));
            mockObject.As<IPersistEntity>().Setup(p => p.EntityLabel).Returns(entityLabel);
            
            return mockObject;
        }

        /// <summary>
        /// Creates a mock IIfcPropertySet with specified name and properties
        /// </summary>
        public static Mock<IIfcPropertySet> CreateMockPropertySet(
            string name, 
            Dictionary<string, string> properties = null)
        {
            var mockPSet = new Mock<IIfcPropertySet>();
            mockPSet.Setup(ps => ps.Name).Returns(name);
            
            if (properties != null && properties.Any())
            {
                var mockProperties = new List<IIfcProperty>();
                foreach (var kvp in properties)
                {
                    var mockProp = CreateMockPropertySingleValue(kvp.Key, kvp.Value);
                    mockProperties.Add(mockProp.Object);
                }
                
                mockPSet.Setup(ps => ps.HasProperties)
                    .Returns(new TestItemSet<IIfcProperty>(mockProperties));
            }
            else
            {
                mockPSet.Setup(ps => ps.HasProperties)
                    .Returns(new TestItemSet<IIfcProperty>(new List<IIfcProperty>()));
            }
            
            return mockPSet;
        }

        /// <summary>
        /// Creates a mock IIfcPropertySingleValue with name and value
        /// </summary>
        public static Mock<IIfcPropertySingleValue> CreateMockPropertySingleValue(
            string name, 
            string value)
        {
            var mockProp = new Mock<IIfcPropertySingleValue>();
            mockProp.Setup(p => p.Name).Returns(name);
            
            var mockValue = new Mock<IIfcValue>();
            mockValue.Setup(v => v.ToString()).Returns(value);
            mockProp.Setup(p => p.NominalValue).Returns(mockValue.Object);
            
            return mockProp;
        }

        /// <summary>
        /// Creates a mock IIfcRelDefinesByProperties relationship
        /// </summary>
        public static Mock<IIfcRelDefinesByProperties> CreateMockRelDefinesByProperties(
            IEnumerable<IIfcPropertySet> propertySets,
            IEnumerable<IIfcObject> relatedObjects)
        {
            var mockRel = new Mock<IIfcRelDefinesByProperties>();
            
            var mockPropertyDefinition = new Mock<IIfcPropertySetDefinitionSelect>();
            mockPropertyDefinition.Setup(pd => pd.PropertySetDefinitions)
                .Returns(new TestItemSet<IIfcPropertySetDefinition>(propertySets.Cast<IIfcPropertySetDefinition>()));
            
            mockRel.Setup(r => r.RelatingPropertyDefinition)
                .Returns(mockPropertyDefinition.Object);
            
            mockRel.Setup(r => r.RelatedObjects)
                .Returns(new TestItemSet<IIfcObjectDefinition>(relatedObjects.Cast<IIfcObjectDefinition>()));
            
            return mockRel;
        }


    }

    /// <summary>
    /// Helper class to simplify ItemSet creation for testing
    /// </summary>
    public class TestItemSet<T> : List<T>, IItemSet<T> where T : class
    {
        public TestItemSet(IEnumerable<T> items) : base(items ?? Enumerable.Empty<T>())
        {
        }

        public T GetAt(int index) => this[index];
        
        public IPersistEntity? OwningEntity { get; set; }
        
        public IPersistEntity? GetAt(long index) => this[(int)index] as IPersistEntity;
        
        public long Count64 => Count;
        
        public new IEnumerable<T> Reverse() => this.AsEnumerable().Reverse();
        
        public T First => this.FirstOrDefault()!;
        
        public T FirstOrDefault(Func<T, bool> predicate) => this.FirstOrDefault(predicate)!;
        
        public TF? FirstOrDefault<TF>(Func<TF, bool> predicate) where TF : T
        {
            return this.OfType<TF>().FirstOrDefault(predicate);
        }
        
        public IEnumerable<TW> Where<TW>(Func<TW, bool> predicate) where TW : T
        {
            return this.OfType<TW>().Where(predicate);
        }
        
        public new void AddRange(IEnumerable<T> items)
        {
            base.AddRange(items);
        }

#pragma warning disable CS0067 // Event is never used
        public event System.Collections.Specialized.NotifyCollectionChangedEventHandler? CollectionChanged;
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
#pragma warning restore CS0067
    }
}
