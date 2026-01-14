namespace TravelioTestConsoleApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //TravelioTestConsoleApp.Aerolinea.ConnectionTest.TestSoapConnectionAsync().GetAwaiter().GetResult();
            TravelioTestConsoleApp.Autos.ConnectionTest.TestBasicGetConnection().GetAwaiter().GetResult();
            //TravelioTestConsoleApp.Habitaciones.ConnectionTest.TestBasicConnectionAsync().GetAwaiter().GetResult();
            //TravelioTestConsoleApp.Mesas.ConnectionTest.TestSoapConnectionAsync().GetAwaiter().GetResult();
            //TravelioTestConsoleApp.Paquetes.ConnectionTest.TestSoapConnectionAsync().GetAwaiter().GetResult();

            //TravelioTestConsoleApp.Banco.BancoTest.RunTransferTest().GetAwaiter().GetResult();
        }
    }
}
