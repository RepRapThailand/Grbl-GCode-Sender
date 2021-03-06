﻿/*
 * DROControl.xaml.cs - part of CNC Controls library
 *
 * v0.02 / 2018-10-17 / Io Engineering (Terje Io)
 *
 */

/*

Copyright (c) 2018-2019, Io Engineering (Terje Io)
All rights reserved.

Redistribution and use in source and binary forms, with or without modification,
are permitted provided that the following conditions are met:

· Redistributions of source code must retain the above copyright notice, this
list of conditions and the following disclaimer.

· Redistributions in binary form must reproduce the above copyright notice, this
list of conditions and the following disclaimer in the documentation and/or
other materials provided with the distribution.

· Neither the name of the copyright holder nor the names of its contributors may
be used to endorse or promote products derived from this software without
specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON
ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

*/

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CNC.Core;

namespace CNC.Controls
{

    public partial class DROControl : UserControl
    {
        private double orgpos;
        private bool hasFocus = false;
        private Brush background = null;
        public string DisplayFormat { get; private set; }

        public delegate void DROEnabledChangedHandler(bool enabled);
        public event DROEnabledChangedHandler DROEnabledChanged;

        public DROControl()
        {
            InitializeComponent();

            foreach (DROBaseControl axis in UIUtils.FindLogicalChildren<DROBaseControl>(this))
            {
                axis.Tag = GrblInfo.AxisLetterToIndex(axis.Label);
                axis.txtReadout.GotFocus += TxtReadout_GotFocus;
                axis.txtReadout.LostFocus += txtPos_LostFocus;
                axis.txtReadout.PreviewKeyUp += txtPos_KeyPress;
                axis.btnZero.Click += btnZero_Click;
            }
        }

        public new bool IsFocused { get { return hasFocus; } }
        public bool IsFocusable { get; set; }

        public void EnableFocus()
        {
            IsFocusable = true;
        }

        private void TxtReadout_GotFocus(object sender, RoutedEventArgs e)
        {
            if (IsFocusable)
            {
                ((GrblViewModel)DataContext).SuspendPositionNotifications = true;

                orgpos = ((GrblViewModel)DataContext).Position.Values[(int)((NumericTextBox)(sender)).Tag];

                background = ((NumericTextBox)(sender)).Background;
                ((NumericTextBox)(sender)).IsReadOnly = false;
                ((NumericTextBox)(sender)).Background = Brushes.White;

                hasFocus = true;

                DROEnabledChanged?.Invoke(true);
            }
        }

        void txtPos_LostFocus(object sender, EventArgs e)
        {
            ((NumericTextBox)(sender)).IsReadOnly = true;

            ((GrblViewModel)DataContext).SuspendPositionNotifications = false;

            if (hasFocus)
            {
                ((NumericTextBox)(sender)).Background = background;
                ((GrblViewModel)DataContext).Position.Values[(int)((NumericTextBox)(sender)).Tag] = orgpos;
            }

            hasFocus = false;

            DROEnabledChanged?.Invoke(false);
        }

        private void txtPos_KeyPress(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                NumericTextBox axis = (NumericTextBox)sender;

                if (axis.Value != orgpos)
                    AxisPositionChanged(GrblInfo.AxisIndexToLetter((int)axis.Tag), axis.Value);

                axis.IsReadOnly = true;

                DROEnabledChanged?.Invoke(false);
            }
        }

        void btnZero_Click(object sender, EventArgs e)
        {
            AxisPositionChanged(GrblInfo.AxisIndexToLetter((int)((Button)sender).Tag), 0.0d);
        }

        void btnZeroAll_Click(object sender, EventArgs e)
        {
            AxisPositionChanged("ALL", 0.0d);
        }

        void AxisPositionChanged(string axis, double position)
        {
            if (axis == "ALL")
            {
                string s = "G90G10L20P0";
                for (int i = 0; i < GrblInfo.NumAxes; i++)
                    s += GrblInfo.AxisIndexToLetter(i) + "{0}";
                Grbl.MDICommand(DataContext, string.Format(s, position.ToInvariantString("F3")));
            }
            else
                Grbl.MDICommand(DataContext, string.Format("G10L20P0{0}{1}", axis, position.ToInvariantString("F3")));
        }

        public void EnableLatheMode()
        {
            if (axisY.IsVisible)
            {
                lblXMode.Visibility = Visibility.Visible;
                axisY.Visibility = Visibility.Collapsed;
            }
        }

        public void SetNumAxes(int numAxes)
        {
            numAxes = Math.Min(numAxes, 6);

            foreach (DROBaseControl axis in UIUtils.FindLogicalChildren<DROBaseControl>(this))
                axis.Visibility = (int)axis.Tag < numAxes ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}


