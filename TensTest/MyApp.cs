using AuxUtils;
using BaseUtils;
using BaseUtils.WindowManagement;
using CommunityToolkit.Mvvm.DependencyInjection;
using System.Windows;

namespace TensTest
{
    public partial class MyApp : AuxBaseApp
    {
        public MyApp(Application app) : base(app)
        {
            System.IO.Directory.SetCurrentDirectory(BaseApp.AppDirectory);
        }
        //(1)
        protected override Window CreateShell()
        {
            var vm = Ioc.Default.GetService<IMainVM>() as MainVM;
            var wnd = WindowLocator.Current.Resolve(vm);
            return wnd;
        }
        //(2) BeforeShow
        //(3) Show
        //(4) AfterShowed
        protected override void AfterShowed()
        {

        }
        //(5)
        protected override void AfterStarted()
        {
            base.AfterStarted();
        }

    }
}
