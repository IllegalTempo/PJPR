# Scale Issue Analysis - Item Pickup/Drop Behavior

## Problem Statement
Items exhibit unexpected ("freaky") scale behavior when being picked up and dropped.

---

## Root Cause Analysis

### Issue 1: Inconsistent Scale Handling in gotPickedup()
**Location**: `gotPickedup()` method, lines ~173-190

**Problem**:
```csharp
if (AbstractItem != null)
{
	ApplySnapshot(AbstractItem.holdState);  // Applies scale from holdState
}
else
{
	transform.localPosition = Vector3.zero;
	transform.localRotation = Quaternion.identity;
	// ❌ NO SCALE RESET - scale remains whatever it was in the scene!
}
```

**Impact**:
- When `AbstractItem` is null, the item's scale is NOT modified
- When `AbstractItem` exists, scale is set to `holdState.scale` (typically `Vector3.one`)
- This creates INCONSISTENT behavior depending on whether AbstractItem is configured

---

### Issue 2: Coordinate Space Mismatch
**Location**: `GetSnapshot()` and `ApplySnapshot()` methods

**Problem**:
```csharp
// GetSnapshot captures:
position = transform.localPosition   // ✓ Local space
rotation = transform.rotation        // ❌ World space (not localRotation!)
scale = transform.localScale         // ✓ Local space

// ApplySnapshot restores:
transform.localPosition = snapshot.position   // ✓ Local space
transform.rotation = snapshot.rotation        // ❌ World space
transform.localScale = snapshot.scale         // ✓ Local space
```

**Impact**:
- Mixed coordinate spaces can cause unexpected rotations when parent transforms change
- When item is reparented to `HandTransform`, world rotation changes can look wrong
- Especially problematic with networked items where parent transform timing varies

---

### Issue 3: Snapshot Capture Timing
**Location**: `OnEnable()` method, line ~63

**Problem**:
```csharp
protected override void OnEnable()
{
	base.OnEnable();
	// ...
	snapshot_start = GetSnapshot();  // Captured during scene initialization
}
```

**Impact**:
- Snapshots are captured in `OnEnable()`, which happens during scene loading
- Scale at this point might include unintended values from editor or previous state
- No guarantee this is the "correct" starting scale

---

### Issue 4: No Scale Lock Option
**Location**: Field `lockRelativeRotation` exists, but no `lockRelativeScale`

**Problem**:
- Rotation locking is available via `lockRelativeRotation` flag
- No equivalent option for scale
- Scale can change unexpectedly without developer control

**Impact**:
- Developers can't easily prevent scale changes
- Inconsistent API (rotation can be locked, scale cannot)

---

### Issue 5: Binding/Unbinding Scale Behavior
**Location**: `Bind()` and `Unbind()` methods, lines ~210-220

**Problem**:
```csharp
public void Bind(slot slot)
{
	snapshot_bind = GetSnapshot();  // Captures current state including scale
	transform.parent = slot.transform;
	transform.localPosition = Vector3.zero;
	transform.rotation = slot.transform.rotation;
	// ❌ Scale is NOT reset here - maintains whatever it was
	BindSlot = slot;
}

public void Unbind()
{
	BindSlot = null;
	ApplySnapshot(snapshot_bind);  // Restores scale from snapshot
}
```

**Impact**:
- If item was scaled while held, binding to a slot preserves that scale
- Unbinding will restore to that modified scale
- Can create visual inconsistencies with slot contents

---

## Potential Solutions

### Solution A: Make Scale Consistent on Pickup (Quick Fix)
**Effort**: Low | **Impact**: Medium

Always reset scale to a sensible default when picking up:

```csharp
private void gotPickedup(PlayerMain who)
{
	// ... existing code ...

	if (AbstractItem != null)
	{
		ApplySnapshot(AbstractItem.holdState);
	}
	else
	{
		transform.localPosition = Vector3.zero;
		transform.localRotation = Quaternion.identity;
		transform.localScale = Vector3.one;  // ✅ Always reset to default
	}
}
```

**Pros**: Simple fix, consistent behavior
**Cons**: Doesn't address coordinate space mismatch

---

### Solution B: Add Scale Lock Option (Medium Fix)
**Effort**: Medium | **Impact**: Medium

Mirror the `lockRelativeRotation` pattern:

```csharp
[SerializeField]
public bool lockRelativeScale = false;

private void gotPickedup(PlayerMain who)
{
	// ... existing code ...

	if (!lockRelativeScale)
	{
		if (AbstractItem != null)
		{
			ApplySnapshot(AbstractItem.holdState);
		}
		else
		{
			transform.localScale = Vector3.one;
		}
	}
	// If lockRelativeScale is true, scale remains unchanged
}
```

**Pros**: Gives developers control, consistent with rotation API
**Cons**: Still doesn't fix coordinate space issues

---

### Solution C: Fix Coordinate Space Consistency (Best Fix)
**Effort**: High | **Impact**: High

Standardize to use LOCAL coordinate space everywhere:

```csharp
private ItemSnapshot GetSnapshot()
{
	return new ItemSnapshot
	{
		position = transform.localPosition,
		rotation = transform.localRotation,  // ✅ Changed to localRotation
		scale = transform.localScale,
	};
}

private void ApplySnapshot(ItemSnapshot snapshot)
{
	transform.localPosition = snapshot.position;
	transform.localRotation = snapshot.rotation;  // ✅ Changed to localRotation
	transform.localScale = snapshot.scale;
}
```

**Pros**: Eliminates coordinate space bugs, cleaner code
**Cons**: May need testing to ensure no networking issues, ItemDefinition holdState uses world rotation

---

### Solution D: Comprehensive Fix (Complete Solution)
**Effort**: High | **Impact**: High

Combine all improvements:

1. Fix coordinate space to use local space consistently
2. Add scale lock option
3. Ensure null AbstractItem case handles scale
4. Add debug logging for scale tracking
5. Add option to preserve or reset scale during binding

---

## Recommendations (Priority Order)

1. **IMMEDIATE** - Apply Solution A (make scale consistent on pickup)
   - Fixes the most obvious inconsistency
   - Low risk, high value

2. **SHORT-TERM** - Apply Solution B (add scale lock option)
   - Gives developers control
   - Consistent with rotation lock API

3. **LONG-TERM** - Apply Solution C (fix coordinate spaces)
   - Eliminates root cause of scale issues
   - Requires careful testing but best long-term fix

4. **OPTIONAL** - Add debug visualization
   - Log scale values through pickup/drop cycle
   - Helps verify fixes are working

---

## Testing Recommendations

After implementing fixes, test these scenarios:

1. **Pickup/Drop Basic**: Item scales correctly when picked up and dropped
2. **Null AbstractItem**: Items without AbstractItem assigned scale consistently
3. **Multiple Pickups**: Picking up and dropping multiple times doesn't cause scale drift
4. **Binding/Unbinding**: Binding to slots and unbinding shows expected scale
5. **Networked Items**: Scale behavior is consistent in multiplayer
6. **Parent Reparenting**: Scale correct after parent changes
7. **Scene Loading**: Scale in editor matches runtime scale

---

## Code References

- `GetSnapshot()` - Line 68
- `ApplySnapshot()` - Line 75
- `gotPickedup()` - Line 163
- `gotDropped()` - Line 195
- `Bind()` - Line 211
- `Unbind()` - Line 220
- `snapshot_start` - Line 51
- `snapshot_bind` - Line 52
