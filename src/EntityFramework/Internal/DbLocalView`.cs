﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.Internal
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;

    /// <summary>
    ///     A local (in-memory) view of the entities in a DbSet.
    ///     This view contains Added entities and does not contain Deleted entities.  The view extends
    ///     from <see cref="ObservableCollection{T}" /> and hooks up events between the collection and the
    ///     state manager to keep the view in sync.
    /// </summary>
    /// <typeparam name="TEntity"> The type of the entity. </typeparam>
    [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix",
        Justification = "Name is intentional")]
    internal class DbLocalView<TEntity> : ObservableCollection<TEntity>
        where TEntity : class
    {
        #region Fields and constructors

        private readonly InternalContext _internalContext;
        private bool _inStateManagerChanged;
        private ObservableBackedBindingList<TEntity> _bindingList;

        /// <summary>
        ///     Initializes a new instance of the <see cref="DbLocalView{TEntity}" /> class for entities
        ///     of the given generic type in the given internal context.
        /// </summary>
        /// <param name="internalContext"> The internal context. </param>
        public DbLocalView(InternalContext internalContext)
        {
            Contract.Requires(internalContext != null);

            _internalContext = internalContext;

            try
            {
                // Set a flag to prevent changes we're making to the ObservableCollection based on the
                // contents of the state manager from being pushed back to the state manager.
                _inStateManagerChanged = true;
                foreach (var entity in _internalContext.GetLocalEntities<TEntity>())
                {
                    Add(entity);
                }
            }
            finally
            {
                _inStateManagerChanged = false;
            }

            _internalContext.RegisterObjectStateManagerChangedEvent(StateManagerChangedHandler);
        }

        #endregion

        #region BindingList

        /// <summary>
        ///     Returns a cached binding list implementation backed by this ObservableCollection.
        /// </summary>
        /// <value> The binding list. </value>
        public ObservableBackedBindingList<TEntity> BindingList
        {
            get { return _bindingList ?? (_bindingList = new ObservableBackedBindingList<TEntity>(this)); }
        }

        #endregion

        #region Change handlers

        /// <summary>
        ///     Called by the <see cref="ObservableCollection{T}" /> base class when the collection changes.
        ///     This method looks at the change made to the collection and reflects those changes in the
        ///     state manager.
        /// </summary>
        /// <param name="e"> The <see cref="System.Collections.Specialized.NotifyCollectionChangedEventArgs" /> instance containing the event data. </param>
        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            Contract.Assert(
                e.Action != NotifyCollectionChangedAction.Reset,
                "Should not get Reset event from our derived implementation of ObservableCollection.");

            // Avoid recursively reacting to changes made to this list while already processing state manager changes.
            // That is, the ObservableCollection only changed because we made a change based on the state manager.
            // We therefore don't want to try to repeat that change in the state manager.
            if (!_inStateManagerChanged)
            {
                if (e.Action == NotifyCollectionChangedAction.Remove
                    || e.Action == NotifyCollectionChangedAction.Replace)
                {
                    foreach (TEntity entity in e.OldItems)
                    {
                        _internalContext.Set<TEntity>().Remove(entity);
                    }
                }

                if (e.Action == NotifyCollectionChangedAction.Add
                    || e.Action == NotifyCollectionChangedAction.Replace)
                {
                    foreach (TEntity entity in e.NewItems)
                    {
                        // For something that is already in the state manager as Unchanged or Modified we don't try
                        // to Add it again since doing so would change its state to Added, which is probably not what
                        // was wanted in this case.
                        if (!_internalContext.EntityInContextAndNotDeleted(entity))
                        {
                            _internalContext.Set<TEntity>().Add(entity);
                        }
                    }
                }
            }
            base.OnCollectionChanged(e);
        }

        /// <summary>
        ///     Handles events from the state manager for entities entering, leaving, or being marked as deleted.
        ///     The local view is kept in sync with these changes.
        /// </summary>
        /// <param name="sender"> The sender. </param>
        /// <param name="e"> The <see cref="System.ComponentModel.CollectionChangeEventArgs" /> instance containing the event data. </param>
        private void StateManagerChangedHandler(object sender, CollectionChangeEventArgs e)
        {
            Contract.Assert(
                e.Action == CollectionChangeAction.Add || e.Action == CollectionChangeAction.Remove,
                "Not expecting Action of Refresh from the state manager");

            try
            {
                // Set a flag to prevent changes we're making to the ObservableCollection based on the
                // contents of the state manager from being pushed back to the state manager.
                _inStateManagerChanged = true;
                var entity = e.Element as TEntity;
                if (entity != null)
                {
                    if (e.Action == CollectionChangeAction.Remove
                        && Contains(entity))
                    {
                        Remove(entity);
                    }
                    else if (e.Action == CollectionChangeAction.Add
                             && !Contains(entity))
                    {
                        Add(entity);
                    }
                }
            }
            finally
            {
                _inStateManagerChanged = false;
            }
        }

        #endregion

        #region Overrides to make ObservableCollection work better with sets of entities

        /// <summary>
        ///     Clears the items by calling remove on each item such that we get Remove events that
        ///     can be tracked back to the state manager, rather than a single Reset event that we
        ///     cannot deal with.
        /// </summary>
        protected override void ClearItems()
        {
            new List<TEntity>(this).ForEach(t => Remove(t));
        }

        /// <summary>
        ///     Adds a contains check to the base implementation of InsertItem since we can't support
        ///     duplicate entities in the set.
        /// </summary>
        /// <param name="index"> The index at which to insert. </param>
        /// <param name="item"> The item to insert. </param>
        protected override void InsertItem(int index, TEntity item)
        {
            if (!Contains(item))
            {
                base.InsertItem(index, item);
            }
        }

        #endregion
    }
}
