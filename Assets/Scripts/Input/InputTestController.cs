using UnityEngine;
using PastryWorld.Input;

public class InputTestController : MonoBehaviour
{
    [SerializeField] private MonoBehaviour _inputProvider;
    private IInputProvider _provider;

    void Start()
    {
        _provider = _inputProvider as IInputProvider;
    }

    void Update()
    {
        if (_provider != null)
        {
            Vector3 pos = _provider.PointerPosition;
            pos.z = Camera.main.farClipPlane - 1f;
            transform.position = Camera.main.ScreenToWorldPoint(pos);
        }
    }
}
