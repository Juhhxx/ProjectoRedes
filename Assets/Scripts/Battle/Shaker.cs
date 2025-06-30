using System.Collections;
using UnityEngine;
using NaughtyAttributes;

public class Shaker : MonoBehaviour
{
    [SerializeField] private float _intensity;
    [SerializeField] private float _duration;
    [SerializeField] private Transform _shakeObject;
    [SerializeField] private Animator _anim;

    private Vector2 _intialPos;
    private YieldInstruction _wff;

    private void Start()
    {
        _intialPos = _shakeObject.position;
        _wff = new WaitForEndOfFrame();
    }

    [Button(enabledMode: EButtonEnableMode.Always)]
    public void DoShake() => StartCoroutine(Shake());
    private IEnumerator Shake()
    {
        float timer = _duration;
        float adjustedIntensity = _intensity;
        Vector2 newPos = Vector2.zero;

        _anim.enabled = false;

        while (timer > 0)
        {
            float rnd = Random.Range(-adjustedIntensity, adjustedIntensity);

            newPos = _intialPos;
            newPos.x += rnd;
            _shakeObject.position = newPos;

            timer -= Time.deltaTime;

            adjustedIntensity = Mathf.Lerp(0, _intensity, timer);

            yield return _wff;
        }

        _shakeObject.position = _intialPos;
        _anim.enabled = true;
    }
}
 