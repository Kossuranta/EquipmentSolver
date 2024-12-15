import { Component } from '@angular/core';
import { FormGroup, FormControl, ReactiveFormsModule } from '@angular/forms';

@Component({
  selector: 'app-add-stat',
  imports: [ReactiveFormsModule],
  templateUrl: './add-stat.component.html',
  styleUrl: './add-stat.component.css'
})
export class AddStatComponent {
  addStatForm = new FormGroup({
    statName: new FormControl(''),
  })

  onSubmit() {
    alert(this.addStatForm.value.statName);
  }
}
