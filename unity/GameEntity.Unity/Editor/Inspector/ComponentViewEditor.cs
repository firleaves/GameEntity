using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;

namespace GameEntity.Unity.Editor
{
    [CustomEditor(typeof(ComponentView))]
    internal sealed class ComponentViewEditor : UnityEditor.Editor
    {
        private static readonly Dictionary<string, bool> FoldoutStates = new Dictionary<string, bool>();

        private string _searchText = string.Empty;
        private bool _showNullValues = true;
        private bool _showProperties;
        private bool _showRawFrameworkFields;

        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new VisualElement();
            ComponentViewInspectorStyles.ApplyRoot(root);
            Rebuild(root);
            return root;
        }

        private void Rebuild(VisualElement root)
        {
            root.Clear();

            ComponentView componentView = (ComponentView)target;
            Entity entity = componentView.Entity;
            ComponentViewInspectorSnapshot snapshot = ComponentViewInspectorSnapshot.From(componentView, entity);

            root.Add(BuildIdentityBlock(snapshot));

            if (entity == null)
            {
                VisualElement emptySection = ComponentViewInspectorStyles.CreateSection("Variables");
                emptySection.Add(ComponentViewInspectorStyles.CreateMutedText("No Entity is bound."));
                root.Add(emptySection);
                return;
            }

            root.Add(BuildInterfacesBlock(snapshot));
            root.Add(BuildVariablesBlock(entity, root));
            root.Add(BuildAdvancedBlock(root));
        }

        private static VisualElement BuildIdentityBlock(ComponentViewInspectorSnapshot snapshot)
        {
            VisualElement section = ComponentViewInspectorStyles.CreateSection("Entity");

            VisualElement nameRow = ComponentViewInspectorStyles.CreateRow("Name");
            TextField nameField = CreateReadOnlyText(snapshot.Name);
            nameRow.Add(nameField);
            nameRow.Add(ComponentViewInspectorStyles.CreateTag(EntityRoleResolver.ToLabel(snapshot.Role), isState: true));
            section.Add(nameRow);

            section.Add(CreateReadOnlyRow("Full Type", snapshot.FullType));
            section.Add(CreateReadOnlyRow("ID", snapshot.Id.ToString()));
            section.Add(CreateReadOnlyRow("Instance ID", snapshot.InstanceId.ToString()));

            return section;
        }

        private static VisualElement BuildInterfacesBlock(ComponentViewInspectorSnapshot snapshot)
        {
            VisualElement section = ComponentViewInspectorStyles.CreateSection("Interfaces");
            VisualElement tags = ComponentViewInspectorStyles.CreateTagList();

            foreach (InterfaceTag tag in snapshot.Interfaces)
            {
                tags.Add(ComponentViewInspectorStyles.CreateTag(tag.Label, tag.IsState, tag.IsWarning));
            }

            if (snapshot.Interfaces.Count == 0)
            {
                tags.Add(ComponentViewInspectorStyles.CreateMutedText("No tracked interfaces."));
            }

            section.Add(tags);
            return section;
        }

        private VisualElement BuildVariablesBlock(Entity entity, VisualElement root)
        {
            VisualElement section = ComponentViewInspectorStyles.CreateSection("Variables");

            ComponentValueElementContext context = new ComponentValueElementContext(
                _searchText,
                showPrivate: true,
                _showNullValues,
                _showProperties,
                _showRawFrameworkFields,
                FoldoutStates);

            IReadOnlyList<ComponentMemberDescriptor> members = ComponentInspectorReflection.GetInspectableMembers(
                entity,
                showPrivate: true,
                _showProperties,
                _showRawFrameworkFields);

            int searchableCount = CountSearchableMembers(
                members,
                new ComponentValueElementContext(
                    string.Empty,
                    showPrivate: true,
                    _showNullValues,
                    _showProperties,
                    _showRawFrameworkFields,
                    FoldoutStates));
            if (ShouldShowSearch(searchableCount))
            {
                section.Add(BuildSearchField(root));
            }

            VisualElement fields = BuildMemberList(members, ComponentMemberKind.Field, context);
            section.Add(fields);

            if (_showProperties)
            {
                VisualElement properties = BuildMemberGroup("Properties", members, ComponentMemberKind.Property, context);
                section.Add(properties);
            }

            return section;
        }

        private VisualElement BuildSearchField(VisualElement root)
        {
            TextField search = new TextField("Search")
            {
                value = _searchText
            };
            search.style.marginBottom = 6;
            search.RegisterValueChangedCallback(evt =>
            {
                _searchText = evt.newValue ?? string.Empty;
                Rebuild(root);
            });

            return search;
        }

        private VisualElement BuildAdvancedBlock(VisualElement root)
        {
            VisualElement section = ComponentViewInspectorStyles.CreateSection("Advanced");
            Foldout foldout = new Foldout
            {
                text = "Options",
                value = FoldoutStates.TryGetValue("advanced.options", out bool value) && value
            };
            foldout.RegisterValueChangedCallback(evt => FoldoutStates["advanced.options"] = evt.newValue);

            foldout.Add(CreateToggle("Show Properties", _showProperties, value =>
            {
                _showProperties = value;
                Rebuild(root);
            }));
            foldout.Add(CreateToggle("Hide Null Values", !_showNullValues, value =>
            {
                _showNullValues = !value;
                Rebuild(root);
            }));
            foldout.Add(CreateToggle("Raw Framework Fields", _showRawFrameworkFields, value =>
            {
                _showRawFrameworkFields = value;
                Rebuild(root);
            }));

            section.Add(foldout);
            return section;
        }

        private bool ShouldShowSearch(int visibleMemberCount)
        {
            return visibleMemberCount > 20 ||
                   _showRawFrameworkFields ||
                   !string.IsNullOrWhiteSpace(_searchText);
        }

        private VisualElement BuildMemberList(
            IReadOnlyList<ComponentMemberDescriptor> members,
            ComponentMemberKind kind,
            ComponentValueElementContext context)
        {
            VisualElement list = new VisualElement();
            int visibleCount = 0;
            foreach (ComponentMemberDescriptor member in members)
            {
                if (member.Kind != kind || !ComponentValueElementFactory.ShouldShowMember(member, context))
                {
                    continue;
                }

                list.Add(ComponentValueElementFactory.CreateMemberElement(member, context));
                visibleCount++;
            }

            if (visibleCount == 0)
            {
                list.Add(ComponentViewInspectorStyles.CreateMutedText(string.IsNullOrWhiteSpace(context.SearchText)
                    ? "No fields."
                    : "No fields match the current filters."));
            }

            return list;
        }

        private static int CountSearchableMembers(
            IReadOnlyList<ComponentMemberDescriptor> members,
            ComponentValueElementContext context)
        {
            int count = 0;
            foreach (ComponentMemberDescriptor member in members)
            {
                if (ComponentValueElementFactory.ShouldShowMember(member, context))
                {
                    count++;
                }
            }

            return count;
        }

        private static VisualElement BuildMemberGroup(
            string title,
            IReadOnlyList<ComponentMemberDescriptor> members,
            ComponentMemberKind kind,
            ComponentValueElementContext context)
        {
            Foldout group = new Foldout
            {
                text = title,
                value = context.GetFoldoutState(title, true)
            };
            group.RegisterValueChangedCallback(evt => context.SetFoldoutState(title, evt.newValue));

            int visibleCount = 0;
            foreach (ComponentMemberDescriptor member in members)
            {
                if (member.Kind != kind || !ComponentValueElementFactory.ShouldShowMember(member, context))
                {
                    continue;
                }

                group.Add(ComponentValueElementFactory.CreateMemberElement(member, context));
                visibleCount++;
            }

            if (visibleCount == 0)
            {
                group.Add(ComponentViewInspectorStyles.CreateMutedText(kind == ComponentMemberKind.Field
                    ? "No fields match the current filters."
                    : "No properties match the current filters."));
            }

            return group;
        }

        private static VisualElement CreateReadOnlyRow(string label, string value)
        {
            VisualElement row = ComponentViewInspectorStyles.CreateRow(label);
            row.Add(CreateReadOnlyText(value));
            return row;
        }

        private static TextField CreateReadOnlyText(string value)
        {
            TextField field = new TextField { value = value ?? string.Empty };
            field.SetEnabled(false);
            field.style.flexGrow = 1;
            return field;
        }

        private static Toggle CreateToggle(string label, bool value, System.Action<bool> onChanged)
        {
            Toggle toggle = new Toggle(label)
            {
                value = value
            };
            toggle.style.marginRight = 10;
            toggle.RegisterValueChangedCallback(evt => onChanged(evt.newValue));
            return toggle;
        }
    }
}
