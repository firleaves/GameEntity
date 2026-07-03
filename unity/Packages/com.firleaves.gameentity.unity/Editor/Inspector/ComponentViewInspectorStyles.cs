using UnityEngine;
using UnityEngine.UIElements;

namespace GameEntity.Unity.Editor
{
    internal static class ComponentViewInspectorStyles
    {
        public static void ApplyRoot(VisualElement root)
        {
            root.style.paddingLeft = 8;
            root.style.paddingRight = 8;
            root.style.paddingTop = 8;
            root.style.paddingBottom = 8;
        }

        public static VisualElement CreateSection(string title)
        {
            VisualElement section = new VisualElement();
            section.style.marginBottom = 8;
            section.style.paddingTop = 7;
            section.style.paddingRight = 8;
            section.style.paddingBottom = 8;
            section.style.paddingLeft = 8;
            section.style.borderTopWidth = 1;
            section.style.borderRightWidth = 1;
            section.style.borderBottomWidth = 1;
            section.style.borderLeftWidth = 1;
            section.style.borderTopColor = new Color(0.22f, 0.22f, 0.22f);
            section.style.borderRightColor = new Color(0.22f, 0.22f, 0.22f);
            section.style.borderBottomColor = new Color(0.22f, 0.22f, 0.22f);
            section.style.borderLeftColor = new Color(0.22f, 0.22f, 0.22f);

            Label header = new Label(title);
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.marginBottom = 6;
            section.Add(header);

            return section;
        }

        public static VisualElement CreateRow(string label)
        {
            VisualElement row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginBottom = 3;

            Label labelElement = new Label(label);
            labelElement.style.minWidth = 108;
            labelElement.style.maxWidth = 148;
            labelElement.style.unityTextAlign = TextAnchor.MiddleLeft;
            labelElement.style.color = new Color(0.7f, 0.7f, 0.7f);
            row.Add(labelElement);

            return row;
        }

        public static Label CreateTag(string text, bool isState = false, bool isWarning = false)
        {
            Label tag = new Label(text);
            tag.style.marginRight = 4;
            tag.style.marginBottom = 4;
            tag.style.paddingLeft = 6;
            tag.style.paddingRight = 6;
            tag.style.paddingTop = 2;
            tag.style.paddingBottom = 2;
            tag.style.borderTopLeftRadius = 3;
            tag.style.borderTopRightRadius = 3;
            tag.style.borderBottomLeftRadius = 3;
            tag.style.borderBottomRightRadius = 3;
            tag.style.borderTopWidth = 1;
            tag.style.borderRightWidth = 1;
            tag.style.borderBottomWidth = 1;
            tag.style.borderLeftWidth = 1;

            Color border = isWarning
                ? new Color(0.72f, 0.43f, 0.22f)
                : isState
                    ? new Color(0.28f, 0.5f, 0.34f)
                    : new Color(0.34f, 0.34f, 0.34f);
            Color background = isWarning
                ? new Color(0.32f, 0.21f, 0.13f)
                : isState
                    ? new Color(0.14f, 0.24f, 0.17f)
                    : new Color(0.18f, 0.18f, 0.18f);

            tag.style.borderTopColor = border;
            tag.style.borderRightColor = border;
            tag.style.borderBottomColor = border;
            tag.style.borderLeftColor = border;
            tag.style.backgroundColor = background;
            return tag;
        }

        public static VisualElement CreateTagList()
        {
            VisualElement tags = new VisualElement();
            tags.style.flexDirection = FlexDirection.Row;
            tags.style.flexWrap = Wrap.Wrap;
            tags.style.flexGrow = 1;
            return tags;
        }

        public static Label CreateMutedText(string text)
        {
            Label label = new Label(text);
            label.style.color = new Color(0.64f, 0.64f, 0.64f);
            label.style.marginTop = 2;
            label.style.marginBottom = 2;
            return label;
        }

        public static Label CreateWarning(string text)
        {
            Label label = new Label(text);
            label.style.color = new Color(1f, 0.68f, 0.35f);
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.marginTop = 2;
            label.style.marginBottom = 2;
            return label;
        }
    }
}
