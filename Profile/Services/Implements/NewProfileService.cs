using BaseBusiness.BO;
using BaseBusiness.Contants;
using BaseBusiness.Model;
using BaseBusiness.util;
using Profile.DTO;
using Profile.Services.Interfaces;

namespace Profile.Services.Implements
{
    public class NewProfileService : INewProfileService
    {
        public ApiResponseAddError<ValidationErrorDto> CreateProfile(SaveProfileRequestDto dto)
        {
            var errors = new List<ValidationErrorDto>();
            DateTime bussinessDate = TextUtils.GetBussinessDateTime();
            try
            {
                ProfileModel profile;
                switch (dto.Type)
                {
                    case 0:
                        errors.AddRange(ValidateIndividual(dto));
                        break;
                    case 1:
                    case 2:
                    case 3:
                        errors.AddRange(ValidateCompany(dto));
                        break;
                    case 4:
                        errors.AddRange(ValidateGroup(dto));
                        break;
                    default:
                        errors.AddRange(ValidateContact(dto));
                        break;
                }

                if (errors.Count != 0)
                    return ValidationFail(errors, dto.Type);

                // Update Profile
                if (dto.ID > 0)
                {
                    if (ProfileBO.Instance.FindByPrimaryKey(dto.ID) is not ProfileModel foundProfile)
                        return ValidationFailNotFound(dto.Type);
                    profile = foundProfile;
                    profile.UpdateDate = bussinessDate;
                    // profile.UpdatedBy = CurrentUserId;
                }

                //Insert Profile
                else
                {
                    profile = new ProfileModel
                    {
                        CreateDate = bussinessDate,
                        UpdateDate = bussinessDate,
                        // CreatedBy = CurrentUserId
                    };
                }
                switch (dto.Type)
                {
                    case 0:
                        MapIndividual(profile, dto);
                        break;
                    case 1:
                    case 2:
                    case 3:
                        MapCompany(profile, dto);
                        break;
                    case 4:
                        MapGroup(profile, dto);
                        break;
                    default:
                        MapContact(profile, dto);
                        break;
                }

                // 4️⃣ Save
                if (dto.ID > 0)
                    ProfileBO.Instance.Update(profile);
                else
                    ProfileBO.Instance.Insert(profile);

                return new ApiResponseAddError<ValidationErrorDto>
                {
                    Success = true,
                    Message = dto.ID > 0
                        ? "Profile updated successfully"
                        : "New profile created successfully",
                    Type = dto.Type
                };
            }
            catch (Exception ex)
            {
                return new ApiResponseAddError<ValidationErrorDto>
                {
                    Success = false,
                    Message = "Unexpected error",
                    Error = ex.Message
                };
            }
        }

        #region Validation
        public static ApiResponseAddError<ValidationErrorDto> ValidationFail(List<ValidationErrorDto> errors, int type)
        {
            return new ApiResponseAddError<ValidationErrorDto>
            {
                Success = false,
                Message = "Validation failed",
                Errors = errors,
                Type = type

            };
        }
        private static ApiResponseAddError<ValidationErrorDto> ValidationFailNotFound(int type)
        {
            return new ApiResponseAddError<ValidationErrorDto>
            {
                Success = false,
                Message = "Profile not found",
                Type = type,
            };
        }


        private static List<ValidationErrorDto> ValidateIndividual(SaveProfileRequestDto dto)
        {
            var errors = new List<ValidationErrorDto>();

            if (string.IsNullOrWhiteSpace(dto.LastNameIndivdual))
                errors.Add(new ValidationErrorDto { Field = "lastNameIndivdual", Message = "Last name not blank" });

            if (string.IsNullOrWhiteSpace(dto.FirstNameIndividual))
                errors.Add(new ValidationErrorDto { Field = "firstNameIndividual", Message = "First name not blank" });

            if (dto.NationalityIndividual == 0)
                errors.Add(new ValidationErrorDto { Field = "nationalityIndividual", Message = "Nationality not blank" });

            if (dto.BlackListIndividual && string.IsNullOrWhiteSpace(dto.BlackListReasonIndividual))
                errors.Add(new ValidationErrorDto { Field = "blackListReasonIndividual", Message = "Reason Black list not blank" });

            errors.AddRange(ValidateCodeLength(dto.CodeIndividual, "codeIndividual", dto.ID));

            return errors;
        }
        private static List<ValidationErrorDto> ValidateContact(SaveProfileRequestDto dto)
        {
            var errors = new List<ValidationErrorDto>();

            if (string.IsNullOrWhiteSpace(dto.LastNameContact))
                errors.Add(new ValidationErrorDto { Field = "lastNameContact", Message = "Last name not blank" });

            if (string.IsNullOrWhiteSpace(dto.FirstNameContact))
                errors.Add(new ValidationErrorDto { Field = "firstNameContact", Message = "First name not blank" });

            errors.AddRange(ValidateCodeLength(dto.CodeContact, "CodeContact", dto.ID));

            return errors;
        }

        private static List<ValidationErrorDto> ValidateCompany(SaveProfileRequestDto dto)
        {
            var errors = new List<ValidationErrorDto>();

            if (string.IsNullOrWhiteSpace(dto.CodeCOM))
                errors.Add(new ValidationErrorDto { Field = "codeCOM", Message = "Code not blank" });

            if (string.IsNullOrWhiteSpace(dto.AccountCOM))
                errors.Add(new ValidationErrorDto { Field = "accountCOM", Message = "Account not blank" });

            if (dto.BlackListCOM && string.IsNullOrWhiteSpace(dto.BlackListReasonCOM))
                errors.Add(new ValidationErrorDto { Field = "blackListReasonCOM", Message = "Reason Black list not blank" });

            errors.AddRange(ValidateCodeLength(dto.CodeCOM, "codeCOM", dto.ID));

            return errors;
        }

        private static List<ValidationErrorDto> ValidateGroup(SaveProfileRequestDto dto)
        {
            var errors = new List<ValidationErrorDto>();

            if (string.IsNullOrWhiteSpace(dto.GroupNameGroup))
                errors.Add(new ValidationErrorDto { Field = "groupNameGroup", Message = "Group Name not blank" });

            errors.AddRange(ValidateCodeLength(dto.CodeGroup, "codeGroup", dto.ID));

            return errors;
        }

        private static List<ValidationErrorDto> ValidateCodeLength(string code, string field, int id)
        {
            var errors = new List<ValidationErrorDto>();

            if (string.IsNullOrWhiteSpace(code))
            {
                errors.Add(new ValidationErrorDto
                {
                    Field = field,
                    Message = "Code not blank"
                });
                return errors;
            }

            if (code.Length != 13)
            {
                errors.Add(new ValidationErrorDto
                {
                    Field = field,
                    Message = "Length Code format!"
                });
            }

            var exists = PropertyUtils
                .ConvertToList<ProfileModel>(
                    ProfileBO.Instance.FindByAttribute("Code", code)
                );

            if (exists != null && exists.Count != 0 && id == 0)
            {
                errors.Add(new ValidationErrorDto
                {
                    Field = field,
                    Message = "Profile code existing in system!"
                });
            }

            return errors;
        }

        #endregion


        #region Mapping

        private static string S(string? value) => value?.Trim() ?? string.Empty;

        private static void MapIndividual(ProfileModel profile, SaveProfileRequestDto dto)
        {
            profile.Type = 0;

            profile.Code = S(dto.CodeIndividual);
            profile.Account = S(dto.AccountIndividual);
            profile.LastName = S(dto.LastNameIndivdual);
            profile.Firstname = S(dto.FirstNameIndividual);
            profile.MiddleName = S(dto.MiddleNameIndividual);

            profile.LanguageID = dto.LanguageIndividual;
            profile.TitleID = dto.TitleIndividual;
            profile.Salutation = S(dto.SalutationIndividual);

            profile.Address = S(dto.AddressIndividual);
            profile.HomeAddress = S(dto.BusAddressIndividual);
            profile.City = S(dto.CityIndividual);
            profile.PostalCode = S(dto.PostalIndividual);
            profile.CountryID = dto.CountryIndividual;
            profile.StateID = dto.StateIndividual;

            profile.VIPID = dto.VIPIndividual;
            profile.VIPReason = S(dto.ReasonIndividual);

            profile.PassPort = S(dto.PassportIndividual);
            profile.Keyword = S(dto.KeywordIndividual);
            profile.DateOfBirth = dto.DobIndividual;

            profile.NationalityID = dto.NationalityIndividual;
            profile.Telephone = S(dto.TelephoneIndividual);
            profile.Email = S(dto.EmailIndividual);
            profile.Website = S(dto.WebsiteIndividual);
            profile.HandPhone = S(dto.HandPhoneIndividual);

            profile.Active = dto.ActiveIndividual;
            profile.Contact = dto.ContactIndividual;
            profile.TaxCode = S(dto.TaxIndividual);

            profile.IsBlackList = dto.BlackListIndividual;
            profile.BlackListReason = dto.BlackListIndividual
                ? S(dto.BlackListReasonIndividual)
                : string.Empty;

            profile.UserInsertID = 136;
            profile.UserUpdateID = 136;
        }
        private static void MapCompany(ProfileModel profile, SaveProfileRequestDto dto)
        {
            profile.Type = dto.Type;

            profile.Code = S(dto.CodeCOM);
            profile.Account = S(dto.AccountCOM);
            profile.FullAccount = S(dto.FullAccount);

            profile.Address = S(dto.AddressCOM);
            profile.HomeAddress = S(dto.BusAddressCOM);
            profile.City = S(dto.CityCOM);
            profile.PostalCode = S(dto.PostalCOM);
            profile.CountryID = dto.CountryCOM;
            profile.StateID = dto.StateCOM;

            profile.Keyword = S(dto.KeywordCOM);
            profile.Description = S(dto.NoteCOM);
            profile.Telephone = S(dto.TelephoneCOM);
            profile.Email = S(dto.EmailCOM);
            profile.Website = S(dto.WebsiteCOM);
            profile.HandPhone = S(dto.HandPhoneCOM);

            profile.Active = dto.ActiveCOM;
            profile.ARNo = S(dto.ARCOM);
            profile.OwnerID = dto.OwnerCOM;
            profile.TerritoryID = dto.TerritoryCOM;
            profile.PersonInChargeID = dto.SaleInChargeCOM;
            profile.AcctContact = S(dto.ContactNameCOM);
            profile.CurrencyID = dto.CurrencyCOM;
            profile.TaxCode = S(dto.TaxCOM);
            profile.MemberType = dto.CompanyTypeCOM;

            profile.IsBlackList = dto.BlackListCOM;
            profile.BlackListReason = dto.BlackListCOM
                ? S(dto.BlackListReasonCOM)
                : string.Empty;

            profile.Company = S(dto.Company2COM);
            profile.BusinessTitle = S(dto.BusinessTitleCOM);
            profile.MarketID = dto.MarketCOM;

            profile.UserInsertID = 136;
            profile.UserUpdateID = 136;
        }
        private static void MapGroup(ProfileModel profile, SaveProfileRequestDto dto)
        {
            profile.Type = 4;

            profile.Code = S(dto.CodeGroup);
            profile.Account = S(dto.GroupNameGroup);

            profile.LanguageID = dto.LanguageGruop;
            profile.Address = S(dto.AddressGroup);
            profile.HomeAddress = S(dto.HomeAddressGroup);
            profile.City = S(dto.CityGroup);
            profile.PostalCode = S(dto.PostalGroup);
            profile.CountryID = dto.CountryGroup;
            profile.StateID = dto.StateGroup;

            profile.VIPID = dto.VIPGroup;
            profile.VIPReason = S(dto.VIPReasonGroup);
            profile.Description = S(dto.NotesGroup);

            profile.Telephone = S(dto.TelephoneGroup);
            profile.Fax = S(dto.FaxGroup);
            profile.Email = S(dto.EmailGroup);
            profile.Website = S(dto.WebsiteGroup);
            profile.HandPhone = S(dto.HandPhoneGroup);

            profile.History = dto.HistoryGroup;
            profile.AcctContact = S(dto.AcctContact);
            profile.CurrencyID = dto.CurrencyGroup;

            profile.UserInsertID = 136;
            profile.UserUpdateID = 136;
        }
        private static void MapContact(ProfileModel profile, SaveProfileRequestDto dto)
        {
            profile.Type = 5;

            profile.Code = S(dto.CodeContact);
            profile.Account = S(dto.AccountContact);
            profile.LastName = S(dto.LastNameContact);
            profile.Firstname = S(dto.FirstNameContact);
            profile.MiddleName = S(dto.MiddleNameContact);

            profile.LanguageID = dto.LanguageContact;
            profile.TitleID = dto.TitleContact;
            profile.Salutation = S(dto.SalutationContact);

            profile.Address = S(dto.AddressContact);
            profile.HomeAddress = S(dto.HomeAddressContact);
            profile.City = S(dto.CityContact);
            profile.PostalCode = S(dto.PostalContact);
            profile.CountryID = dto.CountryContact;
            profile.StateID = dto.StateContact;

            profile.DateOfBirth = dto.DobContact;

            profile.Telephone = S(dto.TelephoneContact);
            profile.Fax = S(dto.FaxContact);
            profile.Email = S(dto.EmaiContact);
            profile.Website = S(dto.WebsiteContact);
            profile.HandPhone = S(dto.HandPhoneContact);

            profile.Position = S(dto.PositionContact);
            profile.Department = S(dto.DeptContact);
            profile.OwnerID = dto.OwnerContact;
            profile.TerritoryID = dto.TerritoryContact;

            profile.UserInsertID = 136;
            profile.UserUpdateID = 136;
        }
        #endregion
    }
}
