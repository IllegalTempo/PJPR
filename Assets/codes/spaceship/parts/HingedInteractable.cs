using UnityEngine;

[RequireComponent(typeof(SpaceshipPart))]
public class HingedInteractable : interactable
{
    private enum LongEdgeAxis
    {
        Auto,
        X,
        Z
    }

    private enum FixedEdge
    {
        Top,
        Bottom,
        Left,
        Right
    }

    private enum EdgeDepth
    {
        Center,
        PositiveSide,
        NegativeSide
    }

    private enum PromptTargetType
    {
        Window,
        Door
    }

    [Header("Animator (Optional)")]
    [SerializeField] private Animator windowAnimator;
    [SerializeField] private string isOpenBoolName = "IsOpen";

    [Header("Trapdoor Flap")]
    [SerializeField] private Transform flap;
    [SerializeField] private float openAngle = 75f;
    [SerializeField] private float openCloseSpeed = 7f;
    [SerializeField] private bool openAwayFromPlayer = true;
    [SerializeField] private LongEdgeAxis longEdgeAxis = LongEdgeAxis.Auto;
    [SerializeField] private FixedEdge fixedEdge = FixedEdge.Top;
    [SerializeField] private EdgeDepth edgeDepth = EdgeDepth.Center;

    [Header("Interaction")]
    [SerializeField] private bool useDirectRaycastFallback = true;
    [SerializeField] private float interactionDistance = 100f;

    [Header("Prompt")]
    [SerializeField] private bool showPrompt = true;
    [SerializeField] private PromptTargetType promptTargetType = PromptTargetType.Window;
    [SerializeField] private string promptText = "{key}: Open/Close {type}";
    [SerializeField] private Vector2 promptSize = new Vector2(320f, 28f);
    [SerializeField] private Vector2 promptOffset = new Vector2(0f, 70f);

    [Header("State")]
    [SerializeField] private bool isOpen;

    private int isOpenBoolHash;
    private bool isLookedAtNow;
    private GUIStyle promptStyle;

    private Quaternion closedLocalRotation;
    private Quaternion targetLocalRotation;
    private Vector3 closedLocalPosition;
    private Vector3 targetLocalPosition;
    private Vector3 hingePivotLocal;

    protected override void OnEnable()
    {
        base.OnEnable();

        if (flap == null)
        {
            flap = transform;
        }

        if (windowAnimator == null)
        {
            windowAnimator = GetComponent<Animator>();
        }

        isOpenBoolHash = Animator.StringToHash(isOpenBoolName);

        closedLocalRotation = flap.localRotation;
        closedLocalPosition = flap.localPosition;

        RebuildTrapdoorTargets();
        ApplyStateImmediate();
        EnsurePromptStyle();
    }

    protected override void Update()
    {
        base.Update();

        isLookedAtNow = IsLookedAt();

        AnimateFlap();
    }

    public override void OnClicked()
    {
        base.OnClicked();
        ToggleWindow();
    }

    public override bool IsFunctionKeyOnly()
    {
        return true;
    }

    [ContextMenu("Toggle Window")]
    public void ToggleWindow()
    {
        isOpen = !isOpen;
        RebuildTrapdoorTargets();
        ApplyStateToAnimator();
    }

    [ContextMenu("Open Window")]
    public void OpenWindow()
    {
        if (!isOpen)
        {
            ToggleWindow();
        }
    }

    [ContextMenu("Close Window")]
    public void CloseWindow()
    {
        if (isOpen)
        {
            ToggleWindow();
        }
    }

    private void RebuildTrapdoorTargets()
    {
        if (flap == null)
        {
            return;
        }

        Vector3 half = GetHalfExtentsLocal();
        bool hingeAxisIsX = ResolveUseXAxisAsHinge(half);

        Vector3 horizontalLongAxisLocal = hingeAxisIsX ? Vector3.right : Vector3.forward;
        Vector3 horizontalOtherAxisLocal = hingeAxisIsX ? Vector3.forward : Vector3.right;
        float horizontalLongExtent = hingeAxisIsX ? half.x : half.z;
        float horizontalOtherExtent = hingeAxisIsX ? half.z : half.x;

        float depthSign = ResolveDepthSign();

        Vector3 axisLocal;
        Vector3 pivotLocal;
        Vector3 oppositeEdgeCenter;
        bool preferUpward = fixedEdge == FixedEdge.Top || fixedEdge == FixedEdge.Bottom;

        if (fixedEdge == FixedEdge.Top || fixedEdge == FixedEdge.Bottom)
        {
            float ySign = fixedEdge == FixedEdge.Top ? 1f : -1f;

            axisLocal = horizontalLongAxisLocal;
            pivotLocal = new Vector3(0f, ySign * half.y, 0f) + (horizontalOtherAxisLocal * (depthSign * horizontalOtherExtent));
            oppositeEdgeCenter = new Vector3(0f, -ySign * half.y, 0f);
        }
        else
        {
            float sideSign = fixedEdge == FixedEdge.Right ? 1f : -1f;

            axisLocal = Vector3.up;
            pivotLocal = (horizontalLongAxisLocal * (sideSign * horizontalLongExtent)) + (horizontalOtherAxisLocal * (depthSign * horizontalOtherExtent));
            oppositeEdgeCenter = horizontalLongAxisLocal * (-sideSign * horizontalLongExtent);
        }

        float signedAngle = ChooseOpenAngle(pivotLocal, axisLocal, oppositeEdgeCenter, preferUpward, Mathf.Abs(openAngle));

        hingePivotLocal = pivotLocal;

        Quaternion delta = Quaternion.AngleAxis(signedAngle, axisLocal);

        targetLocalRotation = isOpen ? closedLocalRotation * delta : closedLocalRotation;

        Vector3 hingeCompensation = closedLocalRotation * (pivotLocal - (delta * pivotLocal));
        targetLocalPosition = isOpen ? closedLocalPosition + hingeCompensation : closedLocalPosition;
    }

    private float ChooseOpenAngle(Vector3 pivotLocal, Vector3 axisLocal, Vector3 oppositeEdgeCenter, bool preferUpward, float magnitude)
    {
        Vector3 awayLocal = GetAwayDirectionLocal();

        float plus = ScoreAngle(pivotLocal, oppositeEdgeCenter, axisLocal, +magnitude, awayLocal, openAwayFromPlayer, preferUpward);
        float minus = ScoreAngle(pivotLocal, oppositeEdgeCenter, axisLocal, -magnitude, awayLocal, openAwayFromPlayer, preferUpward);

        return plus >= minus ? +magnitude : -magnitude;
    }

    private Vector3 GetAwayDirectionLocal()
    {
        Transform cam = GetCameraTransform();
        if (cam == null || flap == null)
        {
            return Vector3.forward;
        }

        Vector3 cameraLocal = flap.InverseTransformPoint(cam.position);
        Vector3 awayLocal = (-cameraLocal).normalized;
        if (awayLocal.sqrMagnitude < 0.0001f)
        {
            awayLocal = Vector3.forward;
        }

        return awayLocal;
    }

    private static float ScoreAngle(Vector3 pivot, Vector3 point, Vector3 axisLocal, float angle, Vector3 awayLocal, bool includeAway, bool preferUpward)
    {
        Quaternion delta = Quaternion.AngleAxis(angle, axisLocal);
        Vector3 moved = pivot + delta * (point - pivot);
        Vector3 displacement = moved - point;

        float upwardWeight = preferUpward ? 10f : 0.25f;
        float upwardScore = displacement.y * upwardWeight;
        float awayScore = includeAway ? Vector3.Dot(displacement, awayLocal) : 0f;
        float moveScore = (Mathf.Abs(displacement.x) + Mathf.Abs(displacement.z)) * 0.05f;
        return upwardScore + awayScore + moveScore;
    }

    private bool ResolveUseXAxisAsHinge(Vector3 half)
    {
        if (longEdgeAxis == LongEdgeAxis.X)
        {
            return true;
        }

        if (longEdgeAxis == LongEdgeAxis.Z)
        {
            return false;
        }

        return half.x >= half.z;
    }

    private float ResolveDepthSign()
    {
        if (edgeDepth == EdgeDepth.PositiveSide)
        {
            return 1f;
        }

        if (edgeDepth == EdgeDepth.NegativeSide)
        {
            return -1f;
        }

        return 0f;
    }

    private void ApplyStateImmediate()
    {
        if (flap == null)
        {
            return;
        }

        flap.localRotation = targetLocalRotation;
        flap.localPosition = targetLocalPosition;
        ApplyStateToAnimator();
    }

    private void AnimateFlap()
    {
        if (windowAnimator != null || flap == null)
        {
            return;
        }

        flap.localRotation = Quaternion.Slerp(flap.localRotation, targetLocalRotation, Time.deltaTime * openCloseSpeed);
        ApplyHingePositionFromCurrentRotation();

        if (Quaternion.Angle(flap.localRotation, targetLocalRotation) < 0.05f)
        {
            flap.localRotation = targetLocalRotation;
            flap.localPosition = targetLocalPosition;
        }
    }

    private void ApplyHingePositionFromCurrentRotation()
    {
        if (flap == null)
        {
            return;
        }

        Quaternion deltaFromClosed = Quaternion.Inverse(closedLocalRotation) * flap.localRotation;
        Vector3 hingeCompensation = closedLocalRotation * (hingePivotLocal - (deltaFromClosed * hingePivotLocal));
        flap.localPosition = closedLocalPosition + hingeCompensation;
    }

    private void ApplyStateToAnimator()
    {
        if (windowAnimator == null)
        {
            return;
        }

        foreach (AnimatorControllerParameter parameter in windowAnimator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Bool && parameter.name == isOpenBoolName)
            {
                windowAnimator.SetBool(isOpenBoolHash, isOpen);
                break;
            }
        }
    }

    private bool IsLookedAt()
    {
        if (!useDirectRaycastFallback)
        {
            return false;
        }

        Transform cameraTransform = GetCameraTransform();
        if (cameraTransform == null)
        {
            return false;
        }

        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
        {
            return false;
        }

        interactable lookedInteractable = hit.collider.GetComponentInParent<interactable>();
        return lookedInteractable == this;
    }

    private Transform GetCameraTransform()
    {
        if (GameCore.instance != null && GameCore.instance.localPlayer != null && GameCore.instance.localPlayer.cam != null)
        {
            return GameCore.instance.localPlayer.cam.transform;
        }

        return Camera.main != null ? Camera.main.transform : null;
    }

    private Vector3 GetHalfExtentsLocal()
    {
        if (flap == null)
        {
            return new Vector3(0.5f, 0.5f, 0.05f);
        }

        MeshFilter meshFilter = flap.GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            Vector3 size = Vector3.Scale(meshFilter.sharedMesh.bounds.size, flap.localScale);
            return size * 0.5f;
        }

        BoxCollider box = flap.GetComponent<BoxCollider>();
        if (box != null)
        {
            Vector3 size = Vector3.Scale(box.size, flap.localScale);
            return size * 0.5f;
        }

        return Vector3.Max(flap.localScale * 0.5f, new Vector3(0.1f, 0.1f, 0.02f));
    }

    private void EnsurePromptStyle()
    {
        if (promptStyle != null)
        {
            return;
        }

        promptStyle = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 14
        };
    }

    private void OnGUI()
    {
        if (!showPrompt || !isLookedAtNow)
        {
            return;
        }

        EnsurePromptStyle();

        Rect rect = new Rect(
            (Screen.width * 0.5f) - (promptSize.x * 0.5f) + promptOffset.x,
            (Screen.height * 0.5f) + promptOffset.y,
            promptSize.x,
            promptSize.y
        );

        GUI.Box(rect, BuildPromptText(), promptStyle);
    }

    private string BuildPromptText()
    {
        string keyLabel = PlayerSettingsMenu.GetFunctionInteractKeyLabel();
        string targetTypeLabel = promptTargetType.ToString();

        if (string.IsNullOrEmpty(promptText))
        {
            return $"{keyLabel}: Open/Close {targetTypeLabel}";
        }

        string formattedText = promptText.Replace("{type}", targetTypeLabel);

        if (formattedText.Contains("{key}"))
        {
            return formattedText.Replace("{key}", keyLabel);
        }

        int colonIndex = formattedText.IndexOf(':');
        if (colonIndex >= 0 && colonIndex < formattedText.Length - 1)
        {
            string actionText = formattedText.Substring(colonIndex + 1).Trim();
            return $"{keyLabel}: {actionText}";
        }

        return $"{keyLabel}: {formattedText}";
    }
}
