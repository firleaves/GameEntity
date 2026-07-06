using System;
using System.Collections.Generic;
using GameEntity;
using UnityEngine;

namespace GameEntity.Unity.Framework
{
    public sealed class UIGroupEntity : Entity, IAwake<UIGroupOptions>, IDestroy
    {
        private readonly List<UIEntity> _entities = new List<UIEntity>();
        private UIEntity _focused;

        public string GroupName { get; private set; }
        public Transform Root { get; private set; }
        public IReadOnlyList<UIEntity> Entities => _entities;

        public void Awake(UIGroupOptions options)
        {
            if (options == null)
            {
                throw new FrameworkException("UIGroup 初始化参数不能为空。");
            }

            GroupName = options.GroupName;
            Root = options.Root;
        }

        public void OnDestroy()
        {
            _entities.Clear();
            _focused = null;
            Root = null;
        }

        public void Add(UIEntity ui, int depth)
        {
            if (ui == null || _entities.Contains(ui))
            {
                return;
            }

            _entities.Add(ui);
            ui.SetGroupAndDepth(GroupName, depth);
            SortByDepth();
            RefreshFocus();
        }

        public void Remove(UIEntity ui)
        {
            if (ui == null)
            {
                return;
            }

            _entities.Remove(ui);
            if (ReferenceEquals(_focused, ui))
            {
                _focused = null;
            }

            RefreshFocus();
        }

        public void Refocus(UIEntity ui)
        {
            if (ui == null || !_entities.Contains(ui))
            {
                return;
            }

            _focused = ui;
            ui.InvokeRefocus();
        }

        public void RefreshFocus()
        {
            if (_entities.Count == 0)
            {
                _focused = null;
                return;
            }

            var top = _entities[_entities.Count - 1];
            for (var i = 0; i < _entities.Count; i++)
            {
                if (ReferenceEquals(_entities[i], top))
                {
                    _entities[i].InvokeReveal();
                }
                else
                {
                    _entities[i].InvokeCover();
                }
            }

            _focused = top;
        }

        private void SortByDepth()
        {
            _entities.Sort((a, b) => a.Depth.CompareTo(b.Depth));
        }
    }

    public sealed class UIGroupOptions
    {
        public string GroupName;
        public Transform Root;
    }
}
