using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Create SpriteManager", fileName = "SpriteManager", order = 0)]
public class SpriteManager : ScriptableObject
{
   static SpriteManager _singleton;
   public static SpriteManager Singleton => _singleton??= Resources.Load<SpriteManager>("SpriteManager");

   public Sprite[] m_fingersIcons;
}
