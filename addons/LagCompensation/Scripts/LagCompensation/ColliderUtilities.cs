using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PG.LagCompensation.Base
{

    /// <summary>
    /// Utilities for node management.
    /// </summary>
    public static class ColliderUtilities
    {
        /// <summary>
        /// Returns an array of all children of the specified type T.
        /// T must inherit from Node.
        /// </summary>
        /// <typeparam name="T">The type of Node to filter by.</typeparam>
        /// <param name="node">The Node whose children are being queried.</param>
        /// <param name="unlimitedDepth">Repeat function for all children of children recursively.</param>
        /// <returns>An array of children of type T.</returns>
        public static List<T> GetChildrenOfType<T>(Node node, bool unlimitedDepth) where T : Node
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));

            List<T> childrenOfType = new List<T>();

            foreach (Node child in node.GetChildren())
            {
                if (child is T typedChild)
                {
                    childrenOfType.Add(typedChild);
                }

                // recursively call function for children and combine lists
                if (unlimitedDepth)
                {
                    var moreChildrenOfType = GetChildrenOfType<T>(child, true);
                    childrenOfType = childrenOfType.Concat(moreChildrenOfType).ToList();
                }
            }

            return childrenOfType;
        }
    }

}