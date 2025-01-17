using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using TsingPigSDK;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace VRAgent
{
    public class SceneAnalyzer : TsingPigSDK.Singleton<SceneAnalyzer>
    {
        /// <summary>
        /// 通过脚本挂载的类对可交互物体进行分类，此处记录可抓取物体的挂载脚本过滤器
        /// </summary>
        public List<string> targetGrabTypeFilter = new List<string>();

        /// <summary>
        /// 所有可抓取实体
        /// </summary>
        public List<IGrabbableEntity> grabbableEntities = new List<IGrabbableEntity>();

        /// <summary>
        /// 所有按钮实体
        /// </summary>
        public List<ITriggerableEntity> triggerableEntities = new List<ITriggerableEntity>();

        /// <summary>
        /// 测试入口
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            targetGrabTypeFilter.Add("XRGrabInteractable");
            targetGrabTypeFilter.Add("Grabbable");
        }

        #region 指标（Metrics）

        private int _curFinishCount = 0;
        public Action RoundFinishEvent;

        /// <summary>
        /// 存储每个实体的触发状态
        /// </summary>
        public Dictionary<IBaseEntity, HashSet<Enum>> entityStates = new Dictionary<IBaseEntity, HashSet<Enum>>();

        /// <summary>
        /// 使用反射，注册所有实体
        /// </summary>
        public void RegisterAllEntities()
        {
            var entityTypes = Assembly.GetExecutingAssembly()
                .GetTypes()
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
            if(!entityStates.ContainsKey(entity))
            {
                entityStates[entity] = new HashSet<Enum>();

                var interfaces = entity.GetType().GetInterfaces();
                foreach(var iface in interfaces)
                {
                    switch(iface)
                    {
                        case Type t when typeof(IGrabbableEntity).IsAssignableFrom(t): grabbableEntities.Add((IGrabbableEntity)entity); break;
                        case Type t when typeof(ITriggerableEntity).IsAssignableFrom(t): triggerableEntities.Add((ITriggerableEntity)entity); break;
                    }
                    var nestedTypes = iface.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    foreach(var nestedType in nestedTypes)
                    {
                        if(nestedType.IsEnum)
                        {
                            var enumValues = Enum.GetValues(nestedType);
                            GetTotalStateCount += enumValues.Length;
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
                    .Add(entity.Name, bold: true, color: Color.yellow)
                    .Add(" Event ", bold: true)
                    .Add(new StackTrace().GetFrame(1).GetMethod().Name, bold: true, color: Color.green)
                    .GetText());
            }
        }

        /// <summary>
        /// 获取总状态个数
        /// </summary>
        /// <param name="enumType"></param>
        /// <returns></returns>
        public int GetTotalTriggeredStateCount
        {
            get
            {
                int res = 0;
                foreach(var v in entityStates.Values)
                {
                    res += v.Count;
                }
                return res;
            }
        }

        public int GetTotalStateCount { get; set; }

        public void ShowMetrics()
        {
            Debug.Log(new RichText()
                .Add("TriggeredStateCount: ", bold: true)
                .Add(GetTotalTriggeredStateCount.ToString(), bold: true, color: Color.yellow)
                .Add(", TotalStateCount: ", bold: true)
                .Add(GetTotalStateCount.ToString(), bold: true, color: Color.yellow));
        }

        public void RoundFinish()
        {
            ShowMetrics();
            _curFinishCount++;
            Debug.Log(new RichText()
                .Add("Round ")
                .Add(_curFinishCount.ToString(), color: Color.yellow, bold: true)
                .Add(" finished"));

            entityStates.Clear();
            GetTotalStateCount = 0;
            RegisterAllEntities();

            RoundFinishEvent?.Invoke();
        }

        #endregion 指标（Metrics）
    }
}