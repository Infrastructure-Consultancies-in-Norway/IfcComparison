using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Security.Cryptography.X509Certificates;
using System.Reflection;
using Xbim.Common;
using Xbim.Common.Enumerations;
using Microsoft.Isam.Esent.Interop;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using IfcComparison.ViewModels;
using Xbim.Common.Exceptions;
using Xbim.IO.Esent;
using System.Collections;
using System.CodeDom;
using System.Windows.Markup.Localizer;
using IfcComparison.Enumerations;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.Kernel;
using Xbim.Ifc4.PropertyResource;
using Xbim.Ifc4.UtilityResource;
using Xbim.Ifc4.MeasureResource;


namespace IfcComparison.Models
{
    public class IfcTools
    {
        public static List<Type> IfcEntities { get; } = GetAllEntities();

        private static List<Type> GetAllEntities()
        {
            ///Boiler code to get assembly loaded in the CLR
            var obj = new object();
            var ifcObj = obj as IIfcBuildingElement;

            var ifcInterfaces = AppDomain.CurrentDomain.GetAssemblies()
                .Where(n => n.FullName.Contains("Xbim.Ifc4"))
                .SelectMany(p => p.GetTypes())
                .Where(t => t.Namespace == "Xbim.Ifc4.Interfaces")
                .ToList();

            return ifcInterfaces;

        }

        private static Type GetIfcEntityInterface(IPersistEntity entity, Type xBimType)
        {
            var ifcInterface = entity.GetType().GetInterfaces()
                .Where(name => name.FullName == xBimType.FullName)
                .Select(p => p)
                .FirstOrDefault();

            if (ifcInterface != null) 
            {
                return ifcInterface;
            }
            else
            {
                return null;
            }
            
        }
        private static List<IPersistEntity> GetModelInstances(IfcStore model, Type xBimType)
        {
            var instances = model.Instances;
            var listInstances = new List<IPersistEntity>();

            foreach (var instance in instances)
            {
                var instanceInterface = GetIfcEntityInterface(instance, xBimType);
                if (instanceInterface != null)
                {
                    if (instanceInterface.Name == xBimType.Name)
                    {
                        listInstances.Add(instance);
                    }

                }

            }

            return listInstances;

        }

        /// <summary>
        /// Method to get all instances from its interface type using reflection. Returns list of IPersistEntities
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <param name="interfaceType"></param>
        /// <returns></returns>
        private static List<T> GetModelInstancesByInvokeReflection<T>(IfcStore model, Type interfaceType)
        {

            #region Also works, but a little more verbose
            //var instances = model.Instances;
            //var method = typeof(IReadOnlyEntityCollection).GetMethod("System.Collections.Generic.IEnumerable`1[TIfc] OfType[TIfc]()");
            ////var methodByName = typeof(IReadOnlyEntityCollection).GetMethod("OfType");

            //var methods = typeof(IReadOnlyEntityCollection).GetMethods();
            //foreach (var met in methods)
            //{
            //    if (met.ToString() == "System.Collections.Generic.IEnumerable`1[TIfc] OfType[TIfc]()")
            //    {
            //        method = met;
            //        var n = met.Name;
            //    }
            //}

            #endregion

            MethodInfo method;
            method = typeof(IReadOnlyEntityCollection).GetMethods()?.FirstOrDefault(
                n => n.Name.Equals("OfType") &&
                n.IsGenericMethod && n.GetParameters().Length == 0);

            MethodInfo generic = method.MakeGenericMethod(interfaceType);
            var instances = (IEnumerable<T>)generic.Invoke(model.Instances, new object[] {});

            return instances.ToList();

        }

        /// <summary>
        /// Not in use
        /// </summary>
        /// <param name="propertySets"></param>
        /// <param name="pSetName"></param>
        /// <returns></returns>
        private static IIfcPropertySet GetPropertySetByName(dynamic propertySets, string pSetName)
        {
            IIfcPropertySet propSet = null;
            //var propSetInput = propertySets as IfcPropertySet;
            foreach (var pSet in propertySets)
            {
                if (pSet.Name == pSetName)
                {
                    propSet = (IIfcPropertySet)pSet;
                }

            }
            if (propSet != null) { return propSet; }
            else{ return null;}
        }
        
        /// <summary>
        /// Get the interface type from the string representation. Only IFC4 - works with IFC2x3 models as well. 
        /// </summary>
        /// <param name="interfaceName"></param>
        /// <returns></returns>
        public static Type GetInterfaceType(string interfaceName)
        {
            Type interfaceType = IfcEntities
                .Where(n => n.Name == interfaceName)
                .Where(n => n.FullName.Contains("Ifc4"))
                .FirstOrDefault();

            return interfaceType;

        }




        /*
        public static string CompareIFCPropertySets(IfcStore oldModel, IfcStore newModel, IfcStore newModelQA, string fileNameSaveAs, string transactionText, ObservableCollection<IfcEntities> ifcEntities, string SomeProp)
        {
            var output = "";

            
            foreach (var entity in ifcEntities)
            {
                var interfaceName = entity.IfcEntity;
                var propSetName = entity.IfcPropertySets;
                var compOperator = entity.ComparisonOperator;
                var compMethod = entity.ComparisonMethod;
                
                var interfaceType = GetInterfaceType(interfaceName.ToString());
                
                //var newInstances = GetModelInstancesByInvokeReflection<IPersistEntity>(newModel, interfaceType);
                var oldInstances = GetModelInstancesByInvokeReflection<IPersistEntity>(oldModel, interfaceType);
                var newInstancesQA = GetModelInstancesByInvokeReflection<IPersistEntity>(newModelQA, interfaceType);

                
                if (compMethod != nameof(ComparisonEnumeration.Identifier))
                {
                    output += $"Comparing entity: {interfaceName}, Pset name: {propSetName} with operator {compOperator}!" + Environment.NewLine;

                    //var oldDict = new Dictionary<IIfcValue, List<IIfcProperty>>();
                    var oldDict = new Dictionary<IIfcValue, (List<IIfcProperty>, List<IIfcObject>)>();
                    //var newDict = new Dictionary<IIfcValue, List<IIfcProperty>>();
                    var newDict = new Dictionary<IIfcValue, (List<IIfcProperty>, List<IIfcObject>)>();

                    //oldDict = GetAllPropSetToDict(oldModel, interfaceType, compOperator, compMethod, propSetName, oldInstances);
                    //using (var cache = oldModel.BeginInverseCaching())
                    {
                        oldDict = GetAllPropSetToDict(oldModel, compOperator, compMethod, propSetName, interfaceName);
                    }
                    //using (var cache = newModelQA.BeginInverseCaching())
                    {
                        newDict = GetAllPropSetToDict(newModelQA, compOperator, compMethod, propSetName, interfaceName);
                    }

                    //Check if key exist
                    var keyNotExistNew = newDict.Keys.Except(oldDict.Keys);
                    output += OutputKeyNotExist(keyNotExistNew, "old");
                    var keyNotExistOld = oldDict.Keys.Except(newDict.Keys);
                    output += OutputKeyNotExist(keyNotExistOld, "new");

                    var keyNewExistInOld = newDict
                        .Where(keyValue => oldDict.Keys.Contains(keyValue.Key))
                        .Select(keyValue => keyValue.Key);

                    //output += CompareIfcObjects<IIfcValue>(oldDict, newDict, newInstancesQA, propSetName, compOperator, newModelQA);
                    output += CompareIfcObjects<IIfcValue>(oldDict, newDict, propSetName, compOperator, compMethod, newModelQA);

                }
                else
                {
                    output += $"Comparing entity: {interfaceName}, Pset name: {propSetName} with {compMethod} and the operator {compOperator} is ignored!" + Environment.NewLine;

                    var oldDict = new Dictionary<IExpressValueType, List<IIfcProperty>>();
                    var newDict = new Dictionary<IExpressValueType, List<IIfcProperty>>();

                    using (var cache = oldModel.BeginInverseCaching())
                    {
                        oldDict = GetAllPropSetToDictId(compOperator, compMethod, propSetName, oldInstances);
                    }
                    using (var cache = newModelQA.BeginInverseCaching())
                    {
                        newDict = GetAllPropSetToDictId(compOperator, compMethod, propSetName, newInstancesQA);
                    }

                    //// IMPLEMENT OVERLOAD WITH NON INVERSE LOOKUP
                    //oldDict = GetAllPropSetToDictId(oldModel, compOperator, compMethod, propSetName, oldInstances);
                    //newDict = GetAllPropSetToDictId(newModelQA, compOperator, compMethod, propSetName, newInstancesQA);

                    //Check if key exist
                    var keyNotExistNew = newDict.Keys.Except(oldDict.Keys);
                    output += OutputKeyNotExist(keyNotExistNew, "old");
                    var keyNotExistOld = oldDict.Keys.Except(newDict.Keys);
                    output += OutputKeyNotExist(keyNotExistOld, "new");

                    var keyNewExistInOld = newDict
                        .Where(keyValue => oldDict.Keys.Contains(keyValue.Key))
                        .Select(keyValue => keyValue.Key);

                    output += CompareIfcObjects<IExpressValueType>(oldDict, newDict, newInstancesQA, propSetName, compOperator, newModelQA);
                    output += "Not implemented!";
                }

            }
            newModelQA.SaveAs(fileNameSaveAs);

            output += "Model Comparison finished!" + Environment.NewLine;
            return output;
        }

        */

        public static string CompareIFCPropertySets(IfcStore oldModel, IfcStore newModel, IfcStore newModelQA, string fileNameSaveAs, string transactionText, ObservableCollection<IfcEntity> ifcEntities)
        {
            var output = "";


            foreach (IfcEntity entity in ifcEntities)
            {
                var ifcComparerer = new IfcComparer(oldModel,
                                                   newModelQA,
                                                   fileNameSaveAs,
                                                   transactionText,
                                                   entity);






            }

            newModelQA.SaveAs(fileNameSaveAs);

            output += "Model Comparison finished!" + Environment.NewLine;
            return output;
        }



        private static string CompareIfcObjects<T>(Dictionary<IIfcValue, (List<IIfcProperty>, List<IIfcObject>)> oldDict, Dictionary<IIfcValue, (List<IIfcProperty>, List<IIfcObject>)> newDict, string pSetName, string compOperator, string compMethod, IfcStore model)
        {
            var output = string.Empty;

            //List<ValueTuple<IPersistEntity, List<IIfcProperty>, List<IIfcProperty>>> data = new List<(IPersistEntity inst, List<IIfcProperty> oldProperties, List<IIfcProperty> newProperties)>();
            var data = new List<(IPersistEntity inst, List<IIfcProperty> oldProperties, List<IIfcProperty> newProperties)>();

            var propsNew = new List<IIfcProperty>();
            var propsOld = new List<IIfcProperty>();
            //IfcValue keyNomValue;
            //IfcGloballyUniqueId id;


            foreach (var item in newDict)
            {
                var key = item.Key;
                propsNew = item.Value.Item1;
                propsOld = oldDict.ContainsKey(key) ? oldDict[key].Item1 : null;
                  
                /*
                if (key.Value.ToString() == "A1-6")
                {
                    ;
                }
                */

                ////////////////////////
                /// FOR DEBUGGING
                //List<string> searchList = new List<string>();
                //searchList.Add("C23-01/7750");
                //searchList.Add("C23-02/7750");
                //searchList.Add("F2-11/6205");

                //var elements = newDict
                //    .Where(k => searchList.Contains(k.Key.Value))
                //    .ToList();

                //foreach (var element in elements)
                //{
                //    //id = item.Value.Item2[0].GlobalId;
                //    var elem = element.Value.Item2;
                //    id = element.Value.Item2[0].GlobalId;
                //}

                /////////////////////

                if (propsOld != null)
                {
                    foreach (var ifcObj in item.Value.Item2)
                    {
                        (IPersistEntity inst, List<IIfcProperty> oldProperties, List<IIfcProperty> newProperties) ifcInstances = ((IPersistEntity)ifcObj, propsOld, propsNew);
                        data.Add(ifcInstances);
                    }

                }

            }

            IPersistEntity persistEntity = null;
            foreach (var instance in data)
            {
                try
                {
                    //open transaction for changes
                    using (var txn = model.BeginTransaction("QA Property Set"))
                    {

                        persistEntity = instance.inst;
                        InsertPropertySet(model, instance.inst, instance.newProperties, instance.oldProperties);

                        // commit changes
                        txn.Commit();
                    }
                }
                catch (Exception e)
                {
                    if (persistEntity == null)
                    {
                        output += $"IfcObject is null {Environment.NewLine} " +
                            $"{e.Message} {Environment.NewLine}";
                        continue;
                    }
                    else
                    {
                        output += $"Error on: {persistEntity} {Environment.NewLine}" +
                            $"{e.Message} {Environment.NewLine}";
                        continue;
                    }
                    //MessageBox.Show(e.Message);
                }

            }



            ////FIX


            return output;
        }
        private static string CompareIfcObjects<T>(Dictionary<T, List<IIfcProperty>> oldDict, Dictionary<T, List<IIfcProperty>> newDict, List<IPersistEntity> newInstancesQA, string pSetName, string compOperator, IfcStore model)
        {
            var output = string.Empty;

            //List<ValueTuple<IPersistEntity, List<IIfcProperty>, List<IIfcProperty>>> data = new List<(IPersistEntity inst, List<IIfcProperty> oldProperties, List<IIfcProperty> newProperties)>();
            var data = new List<(IPersistEntity inst, List<IIfcProperty> oldProperties, List<IIfcProperty> newProperties)>();




            //List<> data;
            foreach (IPersistEntity instance in newInstancesQA)
            {

                var props = GetPropertySetByName(instance, pSetName);
                T keyNomValue;
                var propsNew = new List<IIfcProperty>();
                var propsOld = new List<IIfcProperty>();
                var type = newDict.Keys.FirstOrDefault().GetType();
                if (typeof(IIfcValue).IsAssignableFrom(typeof(T)))
                {
                    var key = GetIfcKey(props, compOperator);
                    keyNomValue = (T)key.NominalValue;
                    propsNew = newDict[keyNomValue];
                    propsOld = oldDict.ContainsKey(keyNomValue) ? oldDict[keyNomValue] : null;
                }
                else
                {
                    var instObj = instance as IIfcObject;
                    var instId = (IExpressValueType)instObj.GlobalId;
                    propsNew = newDict[(T)instId];
                    propsOld = oldDict.ContainsKey((T)instId) ? oldDict[(T)instId] : null;

                }

                if (propsOld != null && propsNew.Count > 0 && propsOld.Count > 0)
                {
                    (IPersistEntity inst, List<IIfcProperty> newProperties, List<IIfcProperty> oldProperties) ifcInstances = (instance, propsNew, propsOld);
                    data.Add(ifcInstances);

                }
            }

            IPersistEntity persistEntity = null;


            foreach (var instance in data)
            {
                try
                {
                    //open transaction for changes
                    using (var txn = model.BeginTransaction("QA Property Set"))
                    {
                        persistEntity = instance.inst;
                        InsertPropertySet(model, instance.inst, instance.newProperties, instance.oldProperties);

                        // commit changes
                        txn.Commit();
                    }
                }
                catch (Exception e)
                {
                    output += persistEntity.ToString() + e.Message + Environment.NewLine;
                    //MessageBox.Show(e.Message);
                }

            }



            ////FIX


            return output;
        }


        /// <summary>
        /// Obsolete method, should be rewritten to just use GeneratePropertySetIfc2x3 directly instead
        /// </summary>
        /// <param name="model"></param>
        /// <param name="instance"></param>
        /// <param name="propsNew"></param>
        /// <param name="propsOld"></param>
        private static void InsertPropertySet(IfcStore model, IPersistEntity instance, List<IIfcProperty> propsNew, List<IIfcProperty> propsOld)
        {
            var newPropSetName = "QA_PSET";
            //if (keyNewExistInOld.Contains(keyNomValue)){propsOld = oldDict[keyNomValue];}
            //else{output += $"{keyNomValue} doesn't exist in old revison. Skipped adding property set." + Environment.NewLine;}

            GeneratePropertySetIfc2x3(model, instance, propsNew, propsOld, newPropSetName);
        }


        /// <summary>
        /// Method to write property comparison value to parameters
        /// </summary>
        /// <param name="model"></param>
        /// <param name="instance"></param>
        /// <param name="propsNew"></param>
        /// <param name="propsOld"></param>
        /// <param name="newPropSetName"></param>
        private static void GeneratePropertySetIfc2x3(IfcStore model, IPersistEntity instance, List<IIfcProperty> propsNew, List<IIfcProperty> propsOld, string newPropSetName)
        {
            // create new property set to host properties
            var pSetRel = model.Instances.New<Xbim.Ifc2x3.Kernel.IfcRelDefinesByProperties>(r =>
            {
                var guid = Guid.NewGuid();
                var globalId = new Xbim.Ifc2x3.UtilityResource.IfcGloballyUniqueId(guid.ToString());
                r.GlobalId = globalId;
                r.RelatingPropertyDefinition = model.Instances.New<Xbim.Ifc2x3.Kernel.IfcPropertySet>(pSet =>
                {
                    pSet.Name = newPropSetName;
                    foreach (IIfcProperty prop in propsNew)
                    {
                        var singleValProp = prop as IIfcPropertySingleValue;
                        IIfcPropertySingleValue oldSingleValProps = null;
                        if (propsOld != null)
                            oldSingleValProps = propsOld
                            .OfType<IIfcPropertySingleValue>()
                            .Where(n => n.Name == singleValProp.Name)
                            .FirstOrDefault();


                        var valToWrite = string.Empty;
                        if (propsOld != null)
                        {
                            if (oldSingleValProps !=null)
                            {
                                //Null check to avoid errors where NominalValue is Null
                                var sValProp = singleValProp.NominalValue?.ToString();
                                var oValProp = oldSingleValProps.NominalValue?.ToString();
                                //Writes Null if one of the old or new value are null
                                if (sValProp == null || oValProp == null)
                                {
                                    valToWrite = "Null";
                                }
                                else if (string.Equals(sValProp, oValProp))
                                {
                                    valToWrite = "Equal";
                                }
                                else
                                {
                                    valToWrite = $"Changed from \"{oldSingleValProps.NominalValue}\" to \"{singleValProp.NominalValue}\"";
                                }

                            }
                            else
                            {
                                valToWrite = $"Changed from \"<undefined>\" to \"{singleValProp.NominalValue}\"";
                            }

                        }
                        else
                        {
                            valToWrite = "N/A";
                        }


                        pSet.HasProperties.Add(model.Instances.New<Xbim.Ifc2x3.PropertyResource.IfcPropertySingleValue>(p =>
                        {
                            p.Name = singleValProp.Name.ToString();
                            p.NominalValue = new Xbim.Ifc2x3.MeasureResource.IfcText(valToWrite);//singleValProp.NominalValue.ToString()); // Default Currency set on IfcProject
                        }));

                    }

                });
            });

            //Add the property set to the instance
            pSetRel.RelatedObjects.Add((Xbim.Ifc2x3.Kernel.IfcObject)instance);
        }


        private static List<IIfcProperty> GetPropertySetByName(IPersistEntity instance, string propSetName)
        {
            IIfcObject instObj;
            List<IIfcProperty> props = null;
            //List<IIfcPropertySingleValue> props;
            if (instance is IIfcObject)
            {
                instObj = instance as IIfcObject;
                props = instObj.IsDefinedBy
                    .SelectMany(rel => rel.RelatingPropertyDefinition.PropertySetDefinitions)
                    .OfType<IIfcPropertySet>()
                    .Where(n => n.Name == propSetName)
                    .SelectMany(pset => pset.HasProperties)
                    .ToList();

            }
            return props;
        }

        private static string OutputKeyNotExist(IEnumerable<IIfcValue> ifcValues, string newOld)
        {
            var outStr = string.Empty;
            foreach (var ifcVal in ifcValues)
            {
                outStr += ifcVal.Value.ToString() + $" does not exist in {newOld} model" + Environment.NewLine;
            }
            return outStr;

        }

        private static string OutputKeyNotExist(IEnumerable<IExpressValueType> ifcValues, string newOld)
        {
            var outStr = string.Empty;
            foreach (var ifcVal in ifcValues)
            {
                outStr += ifcVal.Value.ToString() + $" does not exist in {newOld} model" + Environment.NewLine;
            }
            return outStr;

        }


        public static dynamic ConvertType(dynamic source, Type dest)
        {
            return Convert.ChangeType(source, dest);
        }

        /*
        private static Dictionary<IIfcPropertySingleValue, IItemSet<IIfcProperty>> GetAllPropSetToDict(string comparisonOperator, string comparisonMethod, string propSetName, List<IPersistEntity> instances)
        {
            var dict = new Dictionary<IIfcPropertySingleValue, IItemSet<IIfcProperty>>();

            foreach (var instance in instances)
            {
                var propSets = ((dynamic)instance).PropertySets;
                var propSet = GetPropertySetByName(propSets, propSetName).HasProperties;
                var compOpToUse = GetOperatorToUse(propSet, comparisonOperator);

                IfcLabel key = compOpToUse.NominalValue;

                if (!dict.ContainsKey(compOpToUse.NominalValue))
                {
                    dict.Add(compOpToUse.NominalValue, propSet);
                }

            }
            return dict;

        }
        */
        public static Dictionary<IIfcValue, (List<IIfcProperty>, List<IIfcObject>)> GetAllPropSetToDict(IfcStore model, string comparisonOperator, string comparisonMethod, string propSetName, string interfaceName)
        {
            var dict = new Dictionary<IIfcValue, (List<IIfcProperty>, List<IIfcObject>)>();
            
            foreach (var rel in model.Instances.OfType<IIfcRelDefinesByProperties>())
                //foreach (var rel in model.Instances.OfType<IIfcRelDefinesByProperties>())
            {

                var pset = rel.RelatingPropertyDefinition.PropertySetDefinitions
                        .OfType<IIfcPropertySet>()
                        .Where(n => n.Name == propSetName)
                        .SelectMany(propSet => propSet.HasProperties)
                        .ToList();

                //Check if there's any property sets
                if (pset.Any())
                {
                    var prop = GetIfcKey(pset, comparisonOperator, comparisonMethod);
                    if (prop != null)
                    {

                        /*
                        //DEBUG 
                        if (prop.NominalValue.ToString() == "A1-6")
                        {
                            ;   
                        }
                        */

                        var relObj = rel.RelatedObjects.OfType<IIfcObject>().ToList();
                        relObj.RemoveAll(obj => !interfaceName.Contains(obj.GetType().Name));

                        //Check if there's an related objects with the interface name.
                        if (relObj.Any())
                        {
                            if (dict.ContainsKey(prop?.NominalValue))
                            {
                                continue;
                            }
                            dict.Add(prop.NominalValue, (pset, relObj));
                        }
                    }
                }
                else
                {
                    continue;
                }
            }

            return dict; 

        }
        public static Dictionary<IIfcValue, List<IIfcProperty>> GetAllPropSetToDict(IfcStore model, string comparisonOperator, string comparisonMethod, string propSetName, List<IPersistEntity> instances)
        {
            var dict = new Dictionary<IIfcValue, List<IIfcProperty>>();

            foreach (var rel in model.Instances.OfType<IIfcRelDefinesByProperties>())
                //foreach (var rel in model.Instances.OfType<IIfcRelDefinesByProperties>())
            {

                var relObj = rel.RelatedObjects.OfType<IIfcObject>().ToList();
                foreach (var obj in relObj)
                {

                    var pset = rel.RelatingPropertyDefinition.PropertySetDefinitions
                        .OfType<IIfcPropertySet>()
                        .Where(n => n.Name == propSetName)
                        .SelectMany(propSet => propSet.HasProperties)
                        .ToList();

                    if (pset.Any())
                    {
                        var prop = GetIfcKey(pset, comparisonOperator, comparisonMethod);
                        if (prop != null)
                        {
                            if (dict.ContainsKey(prop?.NominalValue))
                            {
                                continue;
                            }
                            dict.Add(prop.NominalValue, pset);
                        }
                    }
                    else
                    {
                        continue;
                    }
                }

            }

            return dict; 

        }
        private static Dictionary<IIfcValue, List<IIfcProperty>> GetAllPropSetToDict(string comparisonOperator, string comparisonMethod, string propSetName, List<IPersistEntity> instances)
        {
            var dict = new Dictionary<IIfcValue, List<IIfcProperty>>();
            
            foreach (IPersistEntity inst in instances)
            {
                var pset = GetPropertySetByName(inst, propSetName);
                var prop = GetIfcKey(pset, comparisonOperator, comparisonMethod);

                if (!dict.ContainsKey(prop?.NominalValue))
                {
                    dict.Add(prop.NominalValue, pset);
                }
            }
            //Testing(testList);
            return dict; 

        }


        


        private static Dictionary<IExpressValueType, List<IIfcProperty>> GetAllPropSetToDictId(string comparisonOperator, string comparisonMethod, string propSetName, List<IPersistEntity> instances)
        {
            var dict = new Dictionary<IExpressValueType, List<IIfcProperty>>();

            foreach (IPersistEntity inst in instances)
            {
                var pset = GetPropertySetByName(inst, propSetName);
                //var prop = GetIfcKey(pset, comparisonOperator, comparisonMethod);
                IfcGloballyUniqueId instId;
                if (inst is IIfcObject)
                {
                    var instObj = inst as IIfcObject;
                    instId = instObj.GlobalId;

                }

                if (!dict.ContainsKey(instId))
                {
                    dict.Add(instId, pset);
                }
            }
            //Testing(testList);
            return dict;

        }



        private static void Testing(List<List<IIfcProperty>> list)
        {
            int sum = 0;
            var countParam = "K23";

            foreach (List<IIfcProperty> props in list)
            {
                var count = props
                    .OfType<IIfcPropertySingleValue>()
                    .Where(n => n.Name.ToString().Contains(countParam))
                    .FirstOrDefault().NominalValue;

                sum += int.Parse(count.Value.ToString());

            }

            MessageBox.Show($"Total Count: {sum}");


        }

        private static void UpdateDictVals(Dictionary<IIfcValue, List<IIfcProperty>> dict, IIfcPropertySingleValue prop, List<IIfcProperty> pset, IfcStore model)
        {
            List<string> propsToLookup = new List<string>();
            var countParam = "K23";
            var cc = "CC";
            propsToLookup.Add(countParam);
            propsToLookup.Add(cc);



            foreach (string compProp in propsToLookup)
            {
                var propLookup = GetIfcKey(pset, compProp);
                if (prop != null && compProp == "K23")
                {
                    var psetExisting = dict[prop.NominalValue];
                    var nomValToAdd = int.Parse(propLookup.NominalValue.ToString());
                    
                    var retVal = psetExisting
                        .OfType<IIfcPropertySingleValue>()
                        .Where(n => n.Name == propLookup.Name)
                        .FirstOrDefault();

                    using (var txn = model.BeginTransaction("Update dict " + model.FileName))
                    {
                        var existVal = int.Parse(propLookup.NominalValue.ToString());
                        var sum = nomValToAdd + existVal;
                        //retVal.NominalValue = new IfcInteger(sum);
                        txn.Commit();
                    }

                }

            }



        }

        public static IIfcPropertySingleValue GetIfcKey(List<IIfcProperty> ifcProperties, string comparisonOperator, string comparisonMethod = nameof(ComparisonEnumeration.Contains))
        {

            switch (comparisonMethod)
            {
                case nameof(ComparisonEnumeration.Contains):
                    var prop = ifcProperties
                        .OfType<IIfcPropertySingleValue>()
                        .Where(n => n.Name.ToString().Contains(comparisonOperator))
                        .FirstOrDefault();
                    return prop;
                case nameof(ComparisonEnumeration.Exact):
                    prop = ifcProperties
                        .OfType<IIfcPropertySingleValue>()
                        .Where(n => n.Name.ToString() == comparisonOperator)
                        .FirstOrDefault();
                    return prop;
                case nameof(ComparisonEnumeration.Identifier):
                    return prop = null;
                    default:
                    return null;
            }


            //var prop = ifcProperties
            //    .OfType<IIfcPropertySingleValue>()
            //    .Where(n => n.Name.ToString().Contains(comparisonOperator))
            //    .FirstOrDefault();

            //return prop;
        }


        private static IIfcPropertySingleValue GetOperatorToUse(dynamic propertySet, string propValComp)
        {
            IIfcPropertySingleValue returnProp = null;
            foreach (IIfcPropertySingleValue prop in propertySet)
            {
                returnProp = prop.Name.ToString().Contains(propValComp) ? prop : null;
                if (returnProp != null) break;
            }
            return returnProp;
        }


        private static void CreateQAPropSet(IfcStore model, List<IPersistEntity> instances, string newPropSetName, string propSetName)
        {

            foreach (var instance in instances)
            {

                // open transaction for changes
                using (var txn = model.BeginTransaction("QA Property Set"))
                {
                    // create new property set to host properties
                    var pSetRel = model.Instances.New<IfcRelDefinesByProperties>(r =>
                    {
                        var guid = Guid.NewGuid();
                        var globalId = new IfcGloballyUniqueId(guid.ToString());
                        r.GlobalId = globalId;
                        r.RelatingPropertyDefinition = model.Instances.New<IfcPropertySet>(pSet =>
                        {
                            pSet.Name = newPropSetName;
                            pSet.HasProperties.Add(model.Instances.New<IfcPropertySingleValue>(p =>
                            {
                                p.Name = "FKJN";
                                p.NominalValue = new IfcText(""); // Default Currency set on IfcProject
                            }));
                        });
                    });

                    // change the name of the door
                    //rebar.Name += "_costed";
                    // add properties to the door
                    pSetRel.RelatedObjects.Add((IfcObject)instance);

                    // commit changes

                    txn.Commit();

                }
            }

        }



        /*
        public static void ReadIFCTest(string fileName, string fileNameSaveAs, string transactionText, string pSetName, Type xBimType)
        {
            fileName = @"E:\GitHub\QualityAssuranceIFC\QualityAssuranceIFC\3D_K01_f_K_Garverivegen bru.ifc";

            var editor = new XbimEditorCredentials
            {
                ApplicationDevelopersName = "FKJN",
                ApplicationFullName = "QA BIM",
                ApplicationIdentifier = "QA BIM APP",
                ApplicationVersion = "1.0",
                EditorsFamilyName = "Fredrik",
                EditorsGivenName = "Jacobsen",
                EditorsOrganisationName = "COWI AS"
            };

            using (var model = IfcStore.Open(fileName, editor, accessMode: Xbim.IO.XbimDBAccess.ReadWrite))
            {

                //Type GetStaticType<T>(T x) => typeof(T);
                //var staticType = GetStaticType(xBimType);

                ///Unable to Cast type parameter to the interface
                //var IRebars = model.Instances.OfType<IIfcReinforcingBar>();

                var IIfcInstances = GetModelInstances(model, xBimType); 

                foreach (var ifcInstance in IIfcInstances)
                {
                    var propSets = ((dynamic)ifcInstance).PropertySets;
                    IfcPropertySet propSet = GetPropertySetByName(propSets, pSetName);

                }

                if (IIfcInstances.Count() > 0)
                {





                    



                }
                //model.SaveAs(fileName);
            }

            MessageBox.Show("Finished");






            /*
            //string fileName = @"E:\GitHub\QualityAssuranceIFC\QualityAssuranceIFC\House.ifc";
            string fileName = @"E:\COWIgit\COWIBridge\COWIBridge\TestPath\3D_K01_f_K_Garverivegen bru.ifc";


            //const string fileName = "SampleHouse.ifc";
            using (var model = IfcStore.Open(fileName))
            {
                var IRebars = model.Instances.OfType<IIfcReinforcingBar>();

                if (IRebars.Count() > 0 )
                {
                    foreach (IfcReinforcingBar rebar in IRebars)
                    {
                        var props = rebar.PropertySets
                            .Where(p => p.FriendlyName.ToLower() == "merknader")
                            .SelectMany(p => p.HasProperties)
                            .OfType<IIfcPropertySingleValue>()
                            .ToList();

                        foreach (var prop in props)
                        {
                            var propVal = prop;

                        }


                        /*
                        foreach (var IPropSet in rebar.PropertySets)
                        {
                            if (IPropSet.FriendlyName.ToLower() == "merknader")
                            {
                                for (int i = 0; i < IPropSet.HasProperties.Count; i++)
                                {
                                    var prop = IPropSet.HasProperties[i];

                                }
                            }
                        }
                        

                    }


               }
    
            }

        }*/

    }
    }

    

