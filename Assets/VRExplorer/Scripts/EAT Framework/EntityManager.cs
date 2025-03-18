using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TsingPigSDK;
using UnityEngine;

namespace VRExplorer
{
    public class EntityManager : Singleton<EntityManager>
    {

        /// <summary>
        /// VRExplorer的Mono指针
        /// </summary>
        public MonoBehaviour vrexplorerMono;

        /// <summary>
        /// 存储每个Entity的触发状态
        /// </summary>
        public Dictionary<IBaseEntity, HashSet<Enum>> entityStates = new Dictionary<IBaseEntity, HashSet<Enum>>();

        /// <summary>
        /// 存储 mono对应的所有Entity映射
        /// </summary>
        public Dictionary<MonoBehaviour, List<IBaseEntity>> monoEntitiesMapping = new Dictionary<MonoBehaviour, List<IBaseEntity>>();

        /// <summary>
        /// 存储所有mono是否被探索过
        /// </summary>
        public Dictionary<MonoBehaviour, bool> monoState = new Dictionary<MonoBehaviour, bool>();

        /// <summary>
        /// 更新mono状态，并判断是否需要结束测试
        /// </summary>
        /// <param name="mono"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool UpdateMonoState(MonoBehaviour mono, bool value)
        {
            if(mono != null && monoState.ContainsKey(mono))
            {
                monoState[mono] = value;
                if(value && monoState.Values.All(value => value))
                {
                    ExperimentManager.Instance.ExperimentFinish();
                    return true;
                }
                return false;
            }
            return false;
        }

        /// <summary>
        /// 注册所有实体，需要初始化时调用
        /// </summary>
        public void RegisterAllEntities()
        {       
            //var entityTypes = Assembly.Load("Test")
            //    .GetTypes()
            //    .Where(t => typeof(IBaseEntity).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);
            var entityTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(t => typeof(IBaseEntity).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            foreach(var entityType in entityTypes)
            {
                var allEntities = FindObjectsOfType(entityType);
                foreach(var entity in allEntities)
                {
                    RegisterEntity((IBaseEntity)entity);
                }
            }
        }

        /// <summary>
        /// 注册实体并初始化状态
        /// </summary>
        /// <param name="entity"></param>
        private void RegisterEntity(IBaseEntity entity)
        {
            MonoBehaviour mono = entity.transform.GetComponent<MonoBehaviour>();

            if(!monoEntitiesMapping.ContainsKey(mono))
            {
                monoEntitiesMapping[mono] = new List<IBaseEntity>();
            }
            monoEntitiesMapping[mono].Add(entity);
            monoState[mono] = false;

            if(!entityStates.ContainsKey(entity))
            {
                entityStates[entity] = new HashSet<Enum>();

                var interfaces = entity.GetType().GetInterfaces();
                foreach(var iface in interfaces)
                {
                    var nestedTypes = iface.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    foreach(var nestedType in nestedTypes)
                    {
                        if(nestedType.IsEnum)
                        {
                            var enumValues = Enum.GetValues(nestedType);
                            ExperimentManager.Instance.StateCount += enumValues.Length;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 触发实体状态
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="state"></param>
        public void TriggerState(IBaseEntity entity, Enum state)
        {
            if(entityStates.ContainsKey(entity) && !entityStates[entity].Contains(state))
            {
                entityStates[entity].Add(state);
                Debug.Log(new RichText()
                    .Add($"Entity ", bold: true)
                    .Add(entity.Name, bold: true, color: new Color(1.0f, 0.64f, 0.0f))
                    .Add(" State ", bold: true)
                    .Add(state.ToString(), bold: true, color: Color.green)
                    .GetText());
            }
        }

        public T GetEntity<T>(MonoBehaviour mono) where T : class, IBaseEntity
        {
            if(monoEntitiesMapping.TryGetValue(mono, out var entities))
            {
                foreach(var entity in entities)
                {
                    if(entity is T targetEntity)
                    {
                        return targetEntity;
                    }
                }
            }
            Debug.LogError($"{mono} GetEntity result is null");
            return null;
        }
    }
}