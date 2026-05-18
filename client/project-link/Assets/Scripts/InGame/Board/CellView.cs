using UnityEngine;
using ProjectLink.Utils;

namespace ProjectLink.InGame.Board
{
    public class CellView : MonoBehaviour
    {
        SpriteRenderer _renderer;
        Cell _cell;
        bool _highlighted;
        Color _highlightColor;

        static Sprite _sharedSprite;

        public void Init(Cell cell, float cellSize)
        {
            _cell = cell;
            _renderer = gameObject.AddComponent<SpriteRenderer>();
            _renderer.sprite = GetSharedSprite();
            _renderer.sortingLayerName = "Board";
            transform.localScale = Vector3.one * (cellSize * 0.9f);
            Refresh();
        }

        public void SetHighlight(bool highlighted, Color color = default)
        {
            _highlighted = highlighted;
            _highlightColor = color;
            Refresh();
        }

        public void ClearHighlight()
        {
            if (!_highlighted) return;
            _highlighted = false;
            Refresh();
        }

        public void Refresh()
        {
            if (_highlighted)
            {
                _renderer.color = _highlightColor;
                return;
            }
            if (_cell.IsObstacle)
                _renderer.color = new Color(0.3f, 0.3f, 0.3f);
            else if (_cell.IsGimmick)
                _renderer.color = new Color(0.2f, 0.7f, 0.7f);
            else if (_cell.IsNode)
                _renderer.color = ColorPalette.Get(_cell.NodeGroupId);
            else if (_cell.HasPath)
                _renderer.color = ColorPalette.Get(_cell.PathOwner) * new Color(0.65f, 0.65f, 0.65f, 1f);
            else
                _renderer.color = new Color(0.15f, 0.15f, 0.15f);
        }

        static Sprite GetSharedSprite()
        {
            if (_sharedSprite != null) return _sharedSprite;
            var tex = new Texture2D(1, 1) { filterMode = FilterMode.Point };
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            _sharedSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 1f);
            return _sharedSprite;
        }
    }
}
