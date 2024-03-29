// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Common.Utils
{
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    ///     Represents a node in a hierarchical collection of information strings. 
    ///     Intended as a common way mechanism to represent tree structures for debugging (using the TreePrinter class).
    ///     A node consists of a string (represented as a StringBuilder), its collection of child nodes, and an optional Tag value.
    /// </summary>
    internal class TreeNode
    {
        private readonly StringBuilder _text;
        private readonly List<TreeNode> _children = new List<TreeNode>();

        // Default constructor
        internal TreeNode()
        {
            _text = new StringBuilder();
        }

        /// <summary>
        ///     Constructs a new TreeNode with the specified text, tag value and child nodes
        /// </summary>
        /// <param name="text"> The initial value of the new node's text </param>
        /// <param name="children"> An optional list of initial child nodes </param>
        internal TreeNode(string text, params TreeNode[] children)
        {
            if (string.IsNullOrEmpty(text))
            {
                _text = new StringBuilder();
            }
            else
            {
                _text = new StringBuilder(text);
            }

            if (children != null)
            {
                _children.AddRange(children);
            }
        }

        // IEnumerable convenience constructors
        internal TreeNode(string text, List<TreeNode> children)
            : this(text)
        {
            if (children != null)
            {
                _children.AddRange(children);
            }
        }

        // 'public' properties

        /// <summary>
        ///     The current text of this node.
        /// </summary>
        internal StringBuilder Text
        {
            get { return _text; }
        }

        /// <summary>
        ///     The collection of child nodes for this node, which may be empty.
        /// </summary>
        internal IList<TreeNode> Children
        {
            get { return _children; }
        }

        // Used only by the TreePrinter when generating the output string
        internal int Position { get; set; }
    }

    /// <summary>
    ///     Generates a formatted string from a hierarchy of tree nodes. Derived types may override
    ///     the PreProcess, Before/AfterAppend, Print, PrintNode and PrintChildren methods to add
    ///     specific functionality at particular points in process of building the string.
    /// </summary>
    internal abstract class TreePrinter
    {
        #region Private Instance Members

        private readonly List<TreeNode> _scopes = new List<TreeNode>();
        private bool _showLines = true;
        private char _horizontals = '_';
        private char _verticals = '|';

        #endregion

        #region 'Public' API

        /// <summary>
        ///     Entry point method for the TreePrinter
        /// </summary>
        /// <param name="node"> The TreeNode instance that is the root of the tree to be printed </param>
        /// <returns> A string representation of the specified tree </returns>
        internal virtual string Print(TreeNode node)
        {
            PreProcess(node);

            var text = new StringBuilder();
            PrintNode(text, node);
            return text.ToString();
        }

        #endregion

        #region 'Protected' API

        // 'protected' constructor

        // 'protected' API that may be overriden to customize printing

        /// <summary>
        ///     Called once on the root of the tree before printing begins
        /// </summary>
        /// <param name="node"> The TreeNode that is the root of the tree </param>
        internal virtual void PreProcess(TreeNode node)
        {
        }

        /// <summary>
        ///     Called once for every node after indentation, connecting lines and the node's text value
        ///     have been added to the output but before the line suffix (if any) has been added.
        /// </summary>
        /// <param name="node"> The current node </param>
        /// <param name="text"> The StringBuilder into which the tree is being printed </param>
        internal virtual void AfterAppend(TreeNode node, StringBuilder text)
        {
        }

        /// <summary>
        ///     Called once for every node immediately after the line prefix (if any) and appropriate
        ///     indentation and connecting lines have been added to the output but before the node's
        ///     text value has been added.
        /// </summary>
        /// <param name="node"> The current node </param>
        /// <param name="text"> The StringBuilder into which the tree is being printed </param>
        internal virtual void BeforeAppend(TreeNode node, StringBuilder text)
        {
        }

        /// <summary>
        ///     The recursive step of the printing process, called once for each TreeNode in the tree
        /// </summary>
        /// <param name="text"> The StringBuilder into which the tree is being printed </param>
        /// <param name="node"> The current node that should be printed to the StringBuilder </param>
        internal virtual void PrintNode(StringBuilder text, TreeNode node)
        {
            IndentLine(text);

            BeforeAppend(node, text);
            text.Append(node.Text);
            AfterAppend(node, text);

            PrintChildren(text, node);
        }

        /// <summary>
        ///     Called to recursively visit the child nodes of the current TreeNode.
        /// </summary>
        /// <param name="text"> The StringBuilder into which the tree is being printed </param>
        /// <param name="node"> The current node </param>
        internal virtual void PrintChildren(StringBuilder text, TreeNode node)
        {
            _scopes.Add(node);
            node.Position = 0;
            foreach (var childNode in node.Children)
            {
                text.AppendLine();
                node.Position++;
                PrintNode(text, childNode);
            }

            _scopes.RemoveAt(_scopes.Count - 1);
        }

        #endregion

        #region Private Implementation

        private void IndentLine(StringBuilder text)
        {
            var idx = 0;
            for (var scopeIdx = 0; scopeIdx < _scopes.Count; scopeIdx++)
            {
                var parentScope = _scopes[scopeIdx];
                if (!_showLines
                    || (parentScope.Position == parentScope.Children.Count && scopeIdx != _scopes.Count - 1))
                {
                    text.Append(' ');
                }
                else
                {
                    text.Append(_verticals);
                }

                idx++;
                if (_scopes.Count == idx && _showLines)
                {
                    text.Append(_horizontals);
                }
                else
                {
                    text.Append(' ');
                }
            }
        }

        #endregion
    }
}
