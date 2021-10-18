﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Rnd = UnityEngine.Random;

public class RuleOfThreeScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;
    public GameObject[] SphereObjs;
    public GameObject[] CalcObjs;
    public GameObject SpheresParent;
    public GameObject ModuleBackground;
    public KMSelectable ModuleSel;
    public KMSelectable[] MovingSphereSels;
    public Material[] DefaultMats;
    public Material JMat;
    public Material ThreeMat;

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private bool _moduleSolved;

    private float[] _positions = { -0.04f, 0f, 0.04f };
    private float[] _spinningPosX = { 0f, -0.0433f, 0.0433f };
    private float[] _spinningPosZ = { 0.05f, -0.025f, -0.025f };

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

    private bool _canClick;

    private float _currentScale = 0.025f;
    private Coroutine _scaleSpheres;
    private Coroutine _spinSpheres;
    private bool _fullyShrunk;

    private List<int> _answer;
    private List<int> _input;

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        _canClick = true;

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

    private KMSelectable.OnInteractHandler MovingSpherePress(int sphere)
    {
        return delegate ()
        {
            if (_canClick && !_moduleSolved)
            {
                Audio.PlaySoundAtTransform("SphereClick", transform);
                if (_inCyclePhase)
                {
                    _canClick = false;
                    _inCyclePhase = false;
                    _input = new List<int>();
                    for (int i = 0; i < 3; i++)
                    {
                        _sphPos[i] = new int[3];
                        for (int j = 0; j < 3; j++)
                            _sphPos[i][j] = i - 1;
                    }
                }
                else
                {
                    _input.Add(sphere - 1);
                }
            }
            return false;
        };
    }

    private IEnumerator DoSphereCycle()
    {
        while (_inCyclePhase)
        {
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
                    Easing.InOutQuad(elapsed, _positions[xPos[2][i]], _spinningPosX[i], duration),
                    Easing.InOutQuad(elapsed, _positions[yPos[2][i]], 0f, duration),
                    Easing.InOutQuad(elapsed, _positions[zPos[2][i]], _spinningPosZ[i], duration)
                );
            yield return null;
            elapsed += Time.deltaTime;
        }
        yield return new WaitForSeconds(0.5f);
        _spinSpheres = StartCoroutine(SpinSpheres());
        _canClick = true;
    }

    private IEnumerator SpinSpheres()
    {
        _scaleSpheres = StartCoroutine(ScaleSpheres(true));
        while (!_fullyShrunk)
        {
            var duration = 7f;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                SpheresParent.transform.localEulerAngles = new Vector3(0f, Mathf.Lerp(0f, 360f, elapsed / duration), 0f);
                yield return null;
                elapsed += Time.deltaTime;
            }
        }
    }

    private IEnumerator ScaleSpheres(bool shrink)
    {
        if (shrink)
            Audio.PlaySoundAtTransform("ComputerweltStart", transform);
        var duration = shrink ? 14.56f : 0.3f;
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
        if (shrink)
        {
            for (int i = 0; i < 3; i++)
                SphereObjs[i].transform.localScale = new Vector3(0f, 0f, 0f);
            _fullyShrunk = true;
            if (_input.Count != _answer.Count)
                Strike();
            else
            {
                bool correct = true;
                for (int i = 0; i < _input.Count; i++)
                    if (_input[i] != _answer[i])
                        correct = false;
                if (correct)
                {
                    Audio.PlaySoundAtTransform("ComputerweltFinish", transform);
                    Module.HandlePass();
                    _moduleSolved = true;
                    Debug.LogFormat("[Rule of Three #{0}] Inputted {1}. Module solved!", _moduleId, BalTerToString(_input));
                }
                else
                    Strike();
            }
        }
    }

    private void Strike()
    {
        Module.HandleStrike();
        Debug.LogFormat("[Rule of Three #{0}] Inputted {1}. Strike.", _moduleId, BalTerToString(_input));
        _inCyclePhase = true;
        _fullyShrunk = false;
        SpheresParent.transform.localEulerAngles = new Vector3(0f, 0f, 0f);
        if (_scaleSpheres != null)
            StopCoroutine(_scaleSpheres);
        if (_spinSpheres != null)
            StopCoroutine(_spinSpheres);
        for (int i = 0; i < 3; i++)
            SphereObjs[i].transform.localPosition = new Vector3(_positions[xPos[i][0]], _positions[yPos[i][0]], _positions[zPos[i][0]]);
        _scaleSpheres = StartCoroutine(ScaleSpheres(false));
        StartCoroutine(DoSphereCycle());
    }

    private void GenerateSpherePositions()
    {
    tryAgain:
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
        if ((_redCoords[0][0] == _redCoords[1][0] && _redCoords[0][1] == _redCoords[1][1] && _redCoords[0][2] == _redCoords[1][2]) ||
            (_redCoords[0][0] == _redCoords[2][0] && _redCoords[0][1] == _redCoords[2][1] && _redCoords[0][2] == _redCoords[2][2]) ||
            (_redCoords[1][0] == _redCoords[2][0] && _redCoords[1][1] == _redCoords[2][1] && _redCoords[1][2] == _redCoords[2][2]))
        {
            goto tryAgain;
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

        Debug.LogFormat("[Rule of Three #{0}] The area of the triangle formed by these coordinates is {1} square units.", _moduleId, area);
        _answer = DecimalToBalTer(area);
        Debug.LogFormat("[Rule of Three #{0}] The area converted to balanced ternary is {1}.", _moduleId, BalTerToString(_answer));
    }

    private List<int> DecimalToBalTer(int x)
    {
        var i = (int)Math.Ceiling(Math.Log(2 * x + 1) / Math.Log(3));
        var Y = new List<int>();
        while (i > 0)
        {
            if (Math.Abs(x) < ((int)Math.Pow(3, i - 1) + 1) / 2)
            {
                Y.Insert(0, 0);
            }
            else
            {
                if (x > 0)
                {
                    Y.Insert(0, 1);
                    x -= (int)Math.Pow(3, i - 1);
                }
                else
                {
                    Y.Insert(0, -1);
                    x += (int)Math.Pow(3, i - 1);
                }
            }
            i--;
        }
        return Y;
    }

    private int BalTerToDecimal(List<int> balTer)
    {
        int value = 0;
        for (int i = 0; i < balTer.Count; i++)
        {
            value += balTer[i] * (int)Math.Pow(3, i);
        }
        return value;
    }

    private string BalTerToString(List<int> balTer)
    {
        string s = "";
        for (int i = 0; i < balTer.Count; i++)
            s += balTer[i] == -1 ? "-" : balTer[i] == 0 ? "0" : "+";
        if (s == "")
            s = "0";
        return s;
    }
#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Press the colored spheres with “!{0} press red yellow blue” or “!{0} R Y B”";
#pragma warning restore 414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        var m = Regex.Match(command, @"^\s*(?:press |submit)([ryb ,;]+)\s*$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        if (m.Success)
        {
            yield return null;
            if (_inCyclePhase)
                MovingSphereSels[0].OnInteract();
            while (!_canClick)
                yield return null;
            foreach (var btn in m.Groups[1].Value.Where(ch => "rybRYB".Contains(ch)).Select(ch => MovingSphereSels["rybRYB".IndexOf(ch) % 3]))
            {
                btn.OnInteract();
                yield return new WaitForSeconds(0.4f);
            }
            yield break;
        }
        var j = Regex.Match(command, @"^\s*(?:j)+\s*$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        if (j.Success)
        {
            yield return null;
            ModuleBackground.GetComponent<MeshRenderer>().material = JMat;
            yield return "sendtochat j";
        }
        var t = Regex.Match(command, @"^\s*(?:3)+\s*$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        if (t.Success)
        {
            yield return null;
            ModuleBackground.GetComponent<MeshRenderer>().material = ThreeMat;
        }
    }

    private IEnumerator TwitchHandleForcedSolve()
    {
        if (_inCyclePhase)
            MovingSphereSels[0].OnInteract();
        while (!_canClick)
            yield return null;
        _input = new List<int>();
        for (int i = 0; i < _answer.Count; i++)
        {
            MovingSphereSels[_answer[i] + 1].OnInteract();
            yield return new WaitForSeconds(0.4f);
        }
        while (!_moduleSolved)
            yield return true;
    }
}
