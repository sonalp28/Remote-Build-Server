///////////////////////////////////////////////////////////////////////////////////////////////////
// CodePopUp.xaml.cs : Displays text file source in response to double-click                     //
//                                                                                               //
// Platform          : Dell Inspiron 13 - Windows 10, Visual Studio 2017                         //-|_ 
// Language          : C# & .Net Framework                                                       //-|  <----------Requirement 1---------->
// Application       : Project 4 [Build Server] Software Modeling & Analysis CSE-681 Fall'17     //
// Author            : Sonal Patil, Syracuse University                                          //
//                     spatil06@syr.edu (408)-416-6291                                           //  
///////////////////////////////////////////////////////////////////////////////////////////////////
//-----------------------------------------------------------------------------------------------//
//Prologues - 1. public CodePopUp() - Initializes the Components placed in the window 
//-----------------------------------------------------------------------------------------------//
/* Required Files:
* ---------------
* CodePopUp.xaml
*
* Maintenance History:
* --------------------
* ver 1.0 : 19 Nov 2017
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace GUI
{
    /// <summary>
    /// Interaction logic for CodePopUp.xaml
    /// </summary>
    public partial class CodePopUp : Window
    {
        /*-------------------------------------------------< Initializes the Components placed in the window >-------------------------------------------------------*/
        public CodePopUp()
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex) { Console.WriteLine(ex); }
            
        }
    }
}
