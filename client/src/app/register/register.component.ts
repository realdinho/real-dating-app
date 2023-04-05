import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';

@Component({
  selector: 'app-register',
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.css']
})
export class RegisterComponent implements OnInit {
  @Input('users-list') usersList: any;
  @Output() cancelRegister= new EventEmitter();
  model: any = {};

  constructor() { }

  ngOnInit(): void {
    console.log(this.model);
  }

  register() {
    console.log('registered');
  }

  cancel() {
    this.cancelRegister.emit(false);
  }
}
