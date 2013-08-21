using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace TrafficAnalysis.Pages
{
    interface ITabPage
    {
        /// <summary>
        /// Called when parent TabItem is attached.
        /// </summary>
        /// <param name="tItem">The parent TabItem.</param>
        void OnTabItemAttached(MainWindow window, TabItem tItem);
        /// <summary>
        /// Called when item is being detached from parent TabItem.
        /// </summary>
        /// <param name="tItem">The TabItem.</param>
        void OnTabItemDetaching(MainWindow window, TabItem tItem);
        /// <summary>
        /// Gets the parent TabControl.
        /// Should be equal to null if item is not connected to any TabItem.
        /// </summary>
        /// <value>The TabItem.</value>
        TabItem TItem { get; }
        /// <summary>
        /// Gets the Single MainWindow object
        /// </summary>
        /// <value>The MainWindow</value>
        MainWindow Window { get; }
    }
}
