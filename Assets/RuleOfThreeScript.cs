using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Rnd = UnityEngine.Random;

public class RuleOfThreeScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;
    public GameObject[] SphereObjs;
    public GameObject[] CalcObjs;

    public KMSelectable ModuleSel;
    public KMSelectable[] MovingSphereSels;

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private bool _moduleSolved;

    private float[] _positions = { -0.04f, 0f, 0.04f };

    private int[][] zPos = new int[3][];
    private int[][] xPos = new int[3][];
    private int[][] yPos = new int[3][];

    private int[][] _redCoords = new int[3][];
    private int[][] _yellowCoords = new int[3][];
    private int[][] _blueCoords = new int[3][];

    private int[] _redValues = new int[3];
    private int[] _yellowValues = new int[3];
    private int[] _blueValues = new int[3];

    private int[][] _sphPos = new int[3][];

    private bool _inCyclePhase = true;
    private bool _isNegativeMovement = true;

    private string[] _axisNames = { "X", "Y", "Z" };
    private int _axisCycleIx;
    private bool _isMoving;
    private bool _isCycling;

    private float _currentScale = 0.025f;
    private Coroutine _scaleSpheres;

    private void Start()
    {
        _moduleId = _moduleIdCounter++;

        ModuleSel.OnFocus += ModuleFocus;
        ModuleSel.OnDefocus += ModuleDefocus;

        for (int i = 0; i < MovingSphereSels.Length; i++)
            MovingSphereSels[i].OnInteract += MovingSpherePress(i);

        for (int i = 0; i < 3; i++)
        {
            xPos[i] = new int[] { 0, 1, 2 };
            yPos[i] = new int[] { 0, 1, 2 };
            zPos[i] = new int[] { 0, 1, 2 };
            _redCoords[i] = new int[3];
            _yellowCoords[i] = new int[3];
            _blueCoords[i] = new int[3];
        }
        GenerateSpherePositions();
        for (int i = 0; i < 3; i++)
            SphereObjs[i].transform.localPosition = new Vector3(_positions[xPos[i][0]], _positions[yPos[i][0]], _positions[zPos[i][0]]);
        StartCoroutine(DoSphereCycle());
    }

    private void ModuleFocus()
    {
        if (!_isCycling)
        {
            _axisCycleIx = (_axisCycleIx + 1) % 3;
            Debug.LogFormat("[Rule of Three #{0}] After refocusing the module, the axis is now {1}.", _moduleId, _axisNames[_axisCycleIx]);
            if (_scaleSpheres != null)
                StopCoroutine(_scaleSpheres);
            _scaleSpheres = StartCoroutine(ScaleSpheres(true));
        }
    }
    private void ModuleDefocus()
    {
        if (_scaleSpheres != null)
            StopCoroutine(_scaleSpheres);
        StartCoroutine(ScaleSpheres(false));
    }

    private KMSelectable.OnInteractHandler MovingSpherePress(int sphere)
    {
        return delegate ()
        {
            if (_inCyclePhase)
            {
                _inCyclePhase = false;
                for (int i = 0; i < 3; i++)
                {
                    _sphPos[i] = new int[3];
                    for (int j = 0; j < 3; j++)
                        _sphPos[i][j] = i - 1;
                }
                Debug.LogFormat("[Rule of Three #{0}] Moved to input phase. Current axis is {1}", _moduleId, _axisNames[_axisCycleIx]);
            }
            else
            {
                if (!_isMoving)
                    StartCoroutine(MoveSphere(sphere, _axisCycleIx));
            }
            return false;
        };
    }

    private IEnumerator DoSphereCycle()
    {
        while (_inCyclePhase)
        {
            _isCycling = true;
            _isMoving = true;
            for (int j = 0; j < 3; j++)
            {
                var duration = 1f;
                var elapsed = 0f;
                while (elapsed < duration)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        SphereObjs[i].transform.localPosition = new Vector3(
                            Easing.InOutQuad(elapsed, _positions[xPos[(j + 2) % 3][i]], _positions[xPos[j][i]], duration),
                            Easing.InOutQuad(elapsed, _positions[yPos[(j + 2) % 3][i]], _positions[yPos[j][i]], duration),
                            Easing.InOutQuad(elapsed, _positions[zPos[(j + 2) % 3][i]], _positions[zPos[j][i]], duration)
                        );
                    }
                    yield return null;
                    elapsed += Time.deltaTime;
                }
                yield return new WaitForSeconds(0.2f);
            }
            yield return new WaitForSeconds(1f);
            if (!_inCyclePhase)
                StartCoroutine(PrepareSphereMovements());
            _isMoving = false;
        }
    }

    private IEnumerator PrepareSphereMovements()
    {
        var duration = 1.5f;
        var elapsed = 0f;
        while (elapsed < duration)
        {
            for (int i = 0; i < 3; i++)
                SphereObjs[i].transform.localPosition = new Vector3(
                    Easing.InOutQuad(elapsed, _positions[xPos[2][i]], 0.04f * (i - 1), duration),
                    Easing.InOutQuad(elapsed, _positions[yPos[2][i]], 0.04f * (i - 1), duration),
                    Easing.InOutQuad(elapsed, _positions[zPos[2][i]], 0.04f * (i - 1), duration)
                );
            yield return null;
            elapsed += Time.deltaTime;
        }
        _isMoving = false;
        _isCycling = false;
    }

    private IEnumerator MoveSphere(int sph, int axis)
    {
        _isMoving = true;
        var duration = 0.2f;
        var elapsed = 0f;
        while (elapsed < duration)
        {
            if (axis == 0)
            {
                if (_sphPos[sph][0] > 0)
                    _isNegativeMovement = false;
                if (_sphPos[sph][0] < 0)
                    _isNegativeMovement = true;
                SphereObjs[sph].transform.localPosition = new Vector3(
                    Easing.InOutQuad(elapsed, 0.04f * _sphPos[sph][0], !_isNegativeMovement ? 0.04f * (_sphPos[sph][0] - 1) : 0.04f * (_sphPos[sph][0] + 1), duration),
                    0.04f * _sphPos[sph][1],
                    0.04f * _sphPos[sph][2]
                    );
            }
            if (axis == 1)
            {
                if (_sphPos[sph][1] > 0)
                    _isNegativeMovement = false;
                if (_sphPos[sph][1] < 0)
                    _isNegativeMovement = true;
                SphereObjs[sph].transform.localPosition = new Vector3(
                    0.04f * _sphPos[sph][0],
                    Easing.InOutQuad(elapsed, 0.04f * _sphPos[sph][1], !_isNegativeMovement ? 0.04f * (_sphPos[sph][1] - 1) : 0.04f * (_sphPos[sph][1] + 1), duration),
                    0.04f * _sphPos[sph][2]
                    );
            }
            if (axis == 2)
            {
                if (_sphPos[sph][2] > 0)
                    _isNegativeMovement = false;
                if (_sphPos[sph][2] < 0)
                    _isNegativeMovement = true;
                SphereObjs[sph].transform.localPosition = new Vector3(
                    0.04f * _sphPos[sph][0],
                    0.04f * _sphPos[sph][1],
                    Easing.InOutQuad(elapsed, 0.04f * _sphPos[sph][2], !_isNegativeMovement ? 0.04f * (_sphPos[sph][2] - 1) : 0.04f * (_sphPos[sph][2] + 1), duration)
                    );
            }
            yield return null;
            elapsed += Time.deltaTime;
        }
        _sphPos[sph][_axisCycleIx] = !_isNegativeMovement ? _sphPos[sph][_axisCycleIx] - 1 : _sphPos[sph][_axisCycleIx] + 1;
        _isMoving = false;
    }

    private IEnumerator ScaleSpheres(bool shrink)
    {
        var duration = shrink ? 10f : 0.2f;
        var elapsed = 0f;
        var initScale = _currentScale;
        while (elapsed < duration)
        {
            for (int i = 0; i < 3; i++)
                SphereObjs[i].transform.localScale = new Vector3(Mathf.Lerp(initScale, shrink ? 0f : 0.025f, elapsed / duration), Mathf.Lerp(initScale, shrink ? 0f : 0.025f, elapsed / duration), Mathf.Lerp(initScale, shrink ? 0f : 0.025f, elapsed / duration));
            _currentScale = Mathf.Lerp(initScale, shrink ? 0f : 0.025f, elapsed / duration);
            yield return null;
            elapsed += Time.deltaTime;
        }
    }

    private void GenerateSpherePositions()
    {
        for (int i = 0; i < 3; i++)
        {
            xPos[i].Shuffle();
            yPos[i].Shuffle();
            zPos[i].Shuffle();

            _redCoords[i][0] = xPos[i][0] - 1;
            _redCoords[i][1] = yPos[i][0] - 1;
            _redCoords[i][2] = zPos[i][0] - 1;

            _yellowCoords[i][0] = xPos[i][1] - 1;
            _yellowCoords[i][1] = yPos[i][1] - 1;
            _yellowCoords[i][2] = zPos[i][1] - 1;

            _blueCoords[i][0] = xPos[i][2] - 1;
            _blueCoords[i][1] = yPos[i][2] - 1;
            _blueCoords[i][2] = zPos[i][2] - 1;
        }
        Debug.LogFormat("[Rule of Three #{0}] Red coordinates: ({1}, {2}, {3}), ({4}, {5}, {6}), ({7}, {8}, {9}).", _moduleId,
            _redCoords[0][0], _redCoords[0][1], _redCoords[0][2],
            _redCoords[1][0], _redCoords[1][1], _redCoords[1][2],
            _redCoords[2][0], _redCoords[2][1], _redCoords[2][2]
            );
        Debug.LogFormat("[Rule of Three #{0}] Yellow coordinates: ({1}, {2}, {3}), ({4}, {5}, {6}), ({7}, {8}, {9}).", _moduleId,
            _yellowCoords[0][0], _yellowCoords[0][1], _yellowCoords[0][2],
            _yellowCoords[1][0], _yellowCoords[1][1], _yellowCoords[1][2],
            _yellowCoords[2][0], _yellowCoords[2][1], _yellowCoords[2][2]
            );
        Debug.LogFormat("[Rule of Three #{0}] Blue coordinates: ({1}, {2}, {3}), ({4}, {5}, {6}), ({7}, {8}, {9}).", _moduleId,
            _blueCoords[0][0], _blueCoords[0][1], _blueCoords[0][2],
            _blueCoords[1][0], _blueCoords[1][1], _blueCoords[1][2],
            _blueCoords[2][0], _blueCoords[2][1], _blueCoords[2][2]
            );
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                _redValues[i] += (int)Math.Pow(3, j) * _redCoords[j][i];
                _yellowValues[i] += (int)Math.Pow(3, j) * _yellowCoords[j][i];
                _blueValues[i] += (int)Math.Pow(3, j) * _blueCoords[j][i];
            }
        }
        Debug.LogFormat("[Rule of Three #{0}] Red values: {1}, {2}, {3}", _moduleId,
            _redValues[0], _redValues[1], _redValues[2]);
        Debug.LogFormat("[Rule of Three #{0}] Yellow values: {1}, {2}, {3}", _moduleId,
            _yellowValues[0], _yellowValues[1], _yellowValues[2]);
        Debug.LogFormat("[Rule of Three #{0}] Blue values: {1}, {2}, {3}", _moduleId,
            _blueValues[0], _blueValues[1], _blueValues[2]);

        var area = (int)(0.5 * 
            Math.Sqrt(
                Math.Pow((_yellowValues[1] - _redValues[1]) * (_blueValues[2] - _redValues[2]) - (_yellowValues[2] - _redValues[2]) * (_blueValues[1] - _redValues[1]), 2) + 
                Math.Pow((_yellowValues[2] - _redValues[2]) * (_blueValues[0] - _redValues[0]) - (_yellowValues[0] - _redValues[0]) * (_blueValues[2] - _redValues[2]), 2) + 
                Math.Pow((_yellowValues[0] - _redValues[0]) * (_blueValues[1] - _redValues[1]) - (_yellowValues[1] - _redValues[1]) * (_blueValues[0] - _redValues[0]), 2)
            ));
        
        Debug.LogFormat("Area: {0}", area);
    }
}
