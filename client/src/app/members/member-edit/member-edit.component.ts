import { Component, HostListener, OnInit, ViewChild } from '@angular/core';
import { NgForm } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import { take } from 'rxjs';
import { Member } from 'src/app/_models/member';
import { User } from 'src/app/_models/user';
import { AccountService } from 'src/app/_services/account.service';
import { MembersService } from 'src/app/_services/members.service';

@Component({
  selector: 'app-member-edit',
  templateUrl: './member-edit.component.html',
  styleUrls: ['./member-edit.component.css']
})
export class MemberEditComponent implements OnInit {
  @ViewChild('memberEditForm') memberEditForm: NgForm | undefined;
  // browser notification for unsaved changes in the form
  @HostListener('window:beforeunload', ['$event']) unloadNotification($event: any) {
    if (this.memberEditForm?.dirty) {
      $event.returnValue = true;
    }
  }
  member: Member | undefined;
  user: User | null = null;

  constructor(
    private accountService: AccountService,
    private memberService: MembersService,
    private toastr: ToastrService,  
  ) {
    this.accountService.currentUser$.pipe(take(1)).subscribe({
      next: item => this.user = item
    })
  }

  ngOnInit(): void {
    this.loadMember()
  }

  loadMember() {
    if (!this.user) return;
    this.memberService.getMember(this.user.username).subscribe({
      next: item => this.member = item
    })
  }

  updateMember() {
    this.memberService.updateMember(this.memberEditForm?.value).subscribe({
      next: _ => {
        this.toastr.success('Profile updated successfully!');    
        this.memberEditForm?.reset(this.member);
      }
    });
  }
}
