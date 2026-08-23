using System;
using System.Collections.Generic;
using UnityEngine;

namespace PastryWorld.Reality
{
    /// <summary>独白的一行。</summary>
    [Serializable]
    public class MonologueLine
    {
        [Tooltip("说话人（通常是「内心」或物件名）")]
        public string speakerName;

        [TextArea(2, 5)]
        public string text;
    }

    /// <summary>
    /// 现实段落独白数据（T17）。
    /// 现实段落区别于点心世界对话：无分支、无立绘、无奉上，
    /// 是主角对现实物件的内心独白（多段，点击逐段推进）。
    /// </summary>
    [CreateAssetMenu(fileName = "Monologue_New", menuName = "PastryWorld/Reality Monologue")]
    public class RealityMonologueSO : ScriptableObject
    {
        [Tooltip("独白 ID（T20 旗标规则的匹配键）")]
        public string monologueId;

        public List<MonologueLine> lines = new List<MonologueLine>();
    }
}
