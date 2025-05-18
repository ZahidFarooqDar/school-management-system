using AutoMapper;
using SMSServiceModels.Foundation.Base;
using SMSServiceModels.AppUser.Login;
using SMSDomainModels.AppUser.Login;
using SMSDomainModels.Foundation.Base;
namespace SMSFoundation.AutoMapperBindings
{
    public class AutoMapperDefaultProfile : Profile
    {
        public AutoMapperDefaultProfile(IServiceProvider serviceProvider)
        {
            ApplicationSpecificMappings();


            //this.CreateMap<DummySubjectDM, DummySubjectSM>()
            //.ForMember(dst => dst.CreatedOnLTZ, opts => opts.MapFrom(src => DateExtensions.ConvertFromUTCToSystemTimezone(src.CreatedOnUTC)))
            //.ReverseMap();

            //this.CreateMap(typeof(DummySubjectDM), typeof(DummySubjectSM))
            //    .ForMember(nameof(DummySubjectSM.CreatedOnLTZ), opt =>
            //    {
            //        opt.MapFrom("CreatedOnUTC");
            //    });            

            // create auto mapping from DM to SM with same names
            var mapResp = this.RegisterAutoMapperFromDmToSm<SMSDomainModelBase<object>, SMSServiceModelBase<object>>();

            Console.WriteLine("AutoMappings DmToSm Success: " + mapResp.SuccessDmToSmMaps.Count);
            Console.WriteLine("AutoMappings SmToDm Success: " + mapResp.SuccessSmToDmMaps.Count);
            Console.WriteLine("AutoMappings Error: " + mapResp.UnsuccessfullPaths.Count);

            // serviceProviderUsage here
            //.ForMember(
            //    dest => dest.PropertyName,
            //    opt => opt.MapFrom(
            //        s => serviceProvider.GetService<ILanguage>().Language == "en-US"
            //            ? s.PropertyEnglishName
            //            : s.PropertyArabicName));
        }


        private void ApplicationSpecificMappings()
        {
            CreateMap<LoginUserDM, LoginUserSM>();
        }
    }
}
