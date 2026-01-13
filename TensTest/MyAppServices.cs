using AuxUtils.SaveState;
using BaseLog.ViewModels;
using BaseUtils.Mvvm;
using BaseUtils.SaveState;
using CommunityToolkit.Mvvm.DependencyInjection;
using LabBase.ILogging;
using LabEdc.Models;
using LabEft.Interfaces;
using LabEft.Services;
using LabLogger.Models;
using LabLogger.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Navigation.Services;

namespace TensTest
{
    public partial class MyApp
    {
        protected override void ConfigureServices()
        {
            Ioc.Default.ConfigureServices(new ServiceCollection()
                .AddSingleton<ISaveState, SaveState>()
                .AddMySingleton<ILogVM, LogVM>()
                .AddMySingleton<IEdcBase, EdcBase>()
                .AddMySingleton<IMainVM, MainVM>()

                //--- Rozšířené logování z LabBase --------------------------------------------------
                .AddMySingleton<ILogger, Logger>()

                //takto lze pořešit 'ruční' předání parametru do konstruktoru
                //původní:
                //.AddSingleton<ILoggerMessagesVM, LoggerMessagesVM>(provider => new LoggerMessagesVM(true, provider.GetRequiredService<ILogger>()))

                .AddSingleton<LoggerMessagesVM>(sp => new LoggerMessagesVM(true, sp.GetRequiredService<ILogger>()))
                .AddSingleton<ILoggerMessagesVM>(sp => sp.GetRequiredService<LoggerMessagesVM>())

                .AddMySingleton<IViewToViewModelType, ViewToViewModelType>()
                .AddMySingleton<IViewModelFactory, ViewModelFactory>()

                .AddSingleton<NavigationFactory>()
                .AddSingleton<GetNavigationCachedViewFactory>()


                //--- EFT ----------------------------------------------------------------------------
                .AddMySingleton<IEftBase, EftBase>()
                .BuildServiceProvider());


            Ioc.Default.GetService<NavigationFactory>()
                .Add<MainVM, MainWnd>("main");


        }
    }
}
