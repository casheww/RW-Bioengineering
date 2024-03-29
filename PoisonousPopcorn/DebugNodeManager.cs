﻿using System.Collections.Generic;
using RWCustom;
using UnityEngine;

/* Use, modify, redistribute this as you wish. 
 * Create an instance of DebugNodeManager, call its Draw method to Draw.
 * Passing a number of frames to Write will delete the message after that number of frames. 
 * DebugNodeManager lacks some of the features of DebugTextManager (undoing a write/draw, clearing entries) because I don't care enough about them. Feel free to implement yourself and maybe poke me to put it here if you want. */
 

// IMPORTANT !!! RENAME NAMESPACE. Current version of RW BepInEx fails to load duplicate namespaces. Make yours unique to the project.

namespace PoisonousPopcorn.DebuggingHelpers
{
    public class DebugNodeManager
    {
        public DebugNodeManager()
        {
            _nodes = new Dictionary<string, DebugNode>();
        }

        public bool enabled = true;

        public void Update()
        {
            if (!enabled) return;
            
            List<string> toRemove = new List<string>();
            
            foreach (KeyValuePair<string, DebugNode> pair in _nodes)
            {
                if (pair.Value.frames != int.MaxValue)
                {
                    pair.Value.frames--;
                }
                
                if (pair.Value.frames < 1 || pair.Value.dSprite.slatedForDeletetion)
                {
                    toRemove.Add(pair.Key);
                }
            }

            foreach (string key in toRemove)
            {
                _nodes[key].dSprite.RemoveFromRoom();
                _nodes.Remove(key);
            }
        }

        public void Draw(string key, Color color, Room room, IntVector2 pos, float scale = 7.5f,
            int frames = int.MaxValue, string spriteName = "pixel")
        {
            if (!enabled) return;
            
            if (!_nodes.TryGetValue(key, out DebugNode existing) || existing?.dSprite?.sprite == null)
            {
                _nodes[key] = new DebugNode(room, spriteName);
            }

            _nodes[key].dSprite.sprite.color = color;
            _nodes[key].dSprite.pos = room.MiddleOfTile(pos);
            _nodes[key].dSprite.sprite.scale = scale;
            _nodes[key].frames = frames;
        }
        
        private readonly Dictionary<string, DebugNode> _nodes;

        class DebugNode
        {
            public DebugNode(Room room, string spriteName)
            {
                dSprite = new DebugSprite(Vector2.zero, new FSprite(spriteName), room);
                room.AddObject(dSprite);
            }

            public readonly DebugSprite dSprite;
            public int frames;
        }

    }
}