# Bug Report

We found two bugs in [GatienVilain/EscapeGameVR: Virtual reality Escape Game on Unity (github.com)](https://github.com/GatienVilain/EscapeGameVR) and have reported it to the developers (not confirm yet).

## Functional Bug (tool-reported)
After placing three colored blocks and instantiating the bow, triggering the `ArrowControler.cs`'s `ReleaseArrow()` method causes an exception:

> UnityEngine.*Unassigned Reference Exception*:  The variable arrowSpawnPoint of ArrowControler has not been assigned.

- Root Cause:	

    This is due to a missing reference in the bow’s prefab (Assets/Prefabs/Bow/Bow_Wooden_no_string_prefab.prefab), while in ``ArrowControler.cs,` `ReleaseArrow(float strength)`, the code line `arrow.transform.position = arrowSpawnPoint.transform.position;'`it is used.

![img](https://icnw6pwioyaz.feishu.cn/space/api/box/stream/download/asynccode/?code=YzJiODNiZjc4MGQ2OWZlM2NmNzNmMTEzYmZiNmMyOGVfRk9kN1J2aUVsSGFKNWFyYnpSZkNwc1ozbFZSQXc4cGNfVG9rZW46T0ZVOGIwcVZ4b283S294TzFMSWNtNnRibkZGXzE3NDg1MDI5OTM6MTc0ODUwNjU5M19WNA)

- Full Console Information:

```C#
UnityEngine.UnassignedReferenceException: 
    The variable arrowSpawnPoint of ArrowControler has not been assigned.
You probably need to assign the arrowSpawnPoint variable of the ArrowControler script in the inspector.
  at (wrapper managed-to-native) UnityEngine.GameObject.get_transform(UnityEngine.GameObject)
  at ArrowControler.ReleaseArrow (System.Single strength) [0x00026] in D:\--UnityProject\VR\VRExplorer_subjects\EscapeGameVR\Assets\Scripts\bow\ArrowControler.cs:22 
  at UnityEngine.Events.InvokableCall`1[T1].Invoke (T1 args0) [0x00010] in <0c4fef52692340e2a5d42a9b44187fcf>:0 
  at UnityEngine.Events.CachedInvokableCall`1[T].Invoke (System.Object[] args) [0x00001] in <0c4fef52692340e2a5d42a9b44187fcf>:0 
  at UnityEngine.Events.UnityEvent.Invoke () [0x00074] in <0c4fef52692340e2a5d42a9b44187fcf>:0 
  at XRTriggerable.Triggerred () [0x00165] in D:\--UnityProject\VR\VRExplorer\Assets\VRExplorer\Scripts\EAT Framework\Mono\XRTriggerable.cs:70 
UnityEngine.Debug:LogError (object)
XRTriggerable:Triggerred () (at D:/--UnityProject/VR/VRExplorer/Assets/VRExplorer/Scripts/EAT Framework/Mono/XRTriggerable.cs:75)
VRExplorer.TriggerAction/<Execute>d__3:MoveNext () (at D:/--UnityProject/VR/VRExplorer/Assets/VRExplorer/Scripts/EAT Framework/Action/TriggerAction.cs:39)
UnityEngine.UnitySynchronizationContext:ExecuteTasks ()
```



## **Missing Prefab Resource** (tool-assisted + manual):
The prefab(Assets/Prefabs/Bow/Arrow_prefab.prefab). It has been missing.

- Root Cause: 

    The bug occurs because Unity cannot find the prefab file (`Arrow_prefab.prefab`) associated with the stored GUID (`0154d2e39ddf4e04591da0e69d80f7d7`). Possible reasons:

    1. **Deleted/Moved Prefab** – The `.prefab` file was removed or relocated without updating Unity’s references.
    2. **Corrupted/Missing `.meta` File** – The metadata file (containing the GUID) was deleted or modified, breaking the link.
    3. **Version Control Issue** – The `.prefab` or `.meta` file wasn’t properly committed, causing missing references when cloned.
    4. **Manual GUID Change** – The prefab’s GUID was altered (e.g., by file manipulation), making it unreadable.

- Full Console Information:

```Plain
Problem detected while loading the contents of the Prefab file: 'Assets/Prefabs/Bow/Arrow_prefab.prefab'.
Check the following logs for more details.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

Prefab instance problem: Arrow_prefab (Missing Prefab with guid: 0154d2e39ddf4e04591da0e69d80f6d7)
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)
```

![img](https://icnw6pwioyaz.feishu.cn/space/api/box/stream/download/asynccode/?code=ZTJjOTM5Nzg1NjNiMjJkOTYwZmYxOGQ5MmJkZjA1N2VfNmc4OWlpd25ONHd5RzI1aENrTUtYUkhNeE9OVGlEOURfVG9rZW46Sk81OGI1b01kbzlKQTR4SGFTeGNKS1Fmbk9jXzE3NDg1MDMwNjk6MTc0ODUwNjY2OV9WNA)

These bugs are relatively hidden and require completing multiple steps to trigger.